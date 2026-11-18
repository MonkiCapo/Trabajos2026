using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Excepciones;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class PizzaRepositorio : DapperRepo, IPizzaRepositorio
{
    // Proyeccion auxiliar: cada fila es un ingrediente asociado a una pizza.
    // No es una entidad de negocio, asi que queda privada al repositorio.
    private class IngredientePizza
    {
        public int PizzaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    private const string SelectIngredientes = @"
        SELECT pi.pizza_id AS PizzaId, i.nombre AS Nombre
        FROM PIZZA_INGREDIENTE pi
        JOIN INGREDIENTE i ON i.id = pi.ingrediente_id";

    private const string SqlIngredientes = SelectIngredientes + @"
        ORDER BY pi.pizza_id, i.id";

    private const string SqlIngredientesDePizza = SelectIngredientes + @"
        WHERE pi.pizza_id = @PizzaId
        ORDER BY i.id";

    public PizzaRepositorio(IAdo ado) : base(ado) { }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasAsync()
    {
        var sql = "SELECT id, nombre, tamanio, precio FROM PIZZA;";

        using var conexion = NuevaConexion();

        var pizzas = (await conexion.QueryAsync<Pizza>(sql)).ToList();

        // Las dos consultas van sobre la misma conexion: el catalogo y sus
        // ingredientes se leen de forma consistente, sin abrir dos conexiones al pool.
        var ingredientes = await conexion.QueryAsync<IngredientePizza>(SqlIngredientes);

        // ToLookup: cada pizza busca sus ingredientes en O(1), en lugar de
        // recorrer la lista completa de ingredientes por cada pizza.
        var porPizza = ingredientes.ToLookup(ing => ing.PizzaId);

        foreach (var pizza in pizzas)
        {
            pizza.Ingredientes = porPizza[pizza.Id]
                .Select(ing => ing.Nombre)
                .ToList();
        }

        return pizzas;
    }

    public async Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre)
    {
        var sql = "SELECT id, nombre, tamanio, precio FROM PIZZA WHERE nombre = @Nombre;";

        using var conexion = NuevaConexion();

        var pizza = await conexion.QueryFirstOrDefaultAsync<Pizza>(sql, new { Nombre = nombre });
        if (pizza == null) return null;

        var ingredientes = await conexion.QueryAsync<IngredientePizza>(
            SqlIngredientesDePizza, new { PizzaId = pizza.Id });

        pizza.Ingredientes = ingredientes.Select(ing => ing.Nombre).ToList();

        return pizza;
    }

    public async Task<Pizza?> ObtenerPizzaPorIdAsync(int id)
    {
        var sql = "SELECT id, nombre, tamanio, precio FROM PIZZA WHERE id = @Id;";

        using var conexion = NuevaConexion();

        var pizza = await conexion.QueryFirstOrDefaultAsync<Pizza>(sql, new { Id = id });
        if (pizza == null) return null;

        var ingredientes = await conexion.QueryAsync<IngredientePizza>(
            SqlIngredientesDePizza, new { PizzaId = id });

        pizza.Ingredientes = ingredientes.Select(ing => ing.Nombre).ToList();

        return pizza;
    }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasPorNombresAsync(IEnumerable<string> nombres, IDbConnection conexion, IDbTransaction transaction)
    {
        // Consulta unica con IN: el checkout pide todas las pizzas del carrito
        // de una sola vez sobre la conexion transaccional, sin N+1.
        var sql = "SELECT id, nombre, tamanio, precio FROM PIZZA WHERE nombre IN @Nombres;";

        return await conexion.QueryAsync<Pizza>(sql, new { Nombres = nombres }, transaction);
    }

    public async Task<IDictionary<int, List<string>>> ObtenerIngredientesDePizzasAsync(IEnumerable<int> pizzaIds, IDbConnection conexion, IDbTransaction transaction)
    {
        // Ingredientes de las pizzas del pedido en una sola consulta: el
        // servicio valida los "quitar" sin abrir conexiones por pizza.
        var sql = @"
            SELECT pi.pizza_id AS PizzaId, i.nombre AS Nombre
            FROM PIZZA_INGREDIENTE pi
            JOIN INGREDIENTE i ON i.id = pi.ingrediente_id
            WHERE pi.pizza_id IN @PizzaIds
            ORDER BY pi.pizza_id, i.id";

        var filas = await conexion.QueryAsync<IngredientePizza>(sql, new { PizzaIds = pizzaIds }, transaction);

        return filas
            .GroupBy(f => f.PizzaId)
            .ToDictionary(g => g.Key, g => g.Select(f => f.Nombre).ToList());
    }

    public async Task<int> InsertarPizzaAsync(Pizza pizza, IDbConnection conexion, IDbTransaction transaction)
    {
        var sql = @"
            INSERT INTO PIZZA (nombre, tamanio, precio) VALUES (@Nombre, @Tamanio, @Precio);
            SELECT LAST_INSERT_ID();";

        try
        {
            return await conexion.QuerySingleAsync<int>(sql, pizza, transaction);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            // UNIQUE(nombre): carrera entre el chequeo del servicio y el INSERT.
            throw new PizzaYaExisteException($"Ya existe una pizza con el nombre '{pizza.Nombre}'.", ex);
        }
    }

    public async Task ActualizarPizzaAsync(Pizza pizza, IDbConnection conexion, IDbTransaction transaction)
    {
        var sql = "UPDATE PIZZA SET nombre = @Nombre, tamanio = @Tamanio, precio = @Precio WHERE id = @Id;";

        try
        {
            await conexion.ExecuteAsync(sql, pizza, transaction);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new PizzaYaExisteException($"Ya existe una pizza con el nombre '{pizza.Nombre}'.", ex);
        }
    }

    public async Task<int> EliminarPizzaAsync(int id)
    {
        // PIZZA_INGREDIENTE tiene ON DELETE CASCADE: el DELETE limpia solo.
        var sql = "DELETE FROM PIZZA WHERE id = @Id;";

        using var conexion = NuevaConexion();

        try
        {
            return await conexion.ExecuteAsync(sql, new { Id = id });
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            // ITEM_PEDIDO.pizza_id es FK RESTRICT: hay pedidos que la usan.
            throw new PizzaEnUsoException("La pizza tiene pedidos asociados y no se puede eliminar.", ex);
        }
    }

    public async Task ReemplazarIngredientesAsync(int pizzaId, IEnumerable<int> ingredienteIds, IDbConnection conexion, IDbTransaction transaction)
    {
        await conexion.ExecuteAsync(
            "DELETE FROM PIZZA_INGREDIENTE WHERE pizza_id = @PizzaId;",
            new { PizzaId = pizzaId }, transaction);

        foreach (var ingredienteId in ingredienteIds)
        {
            await conexion.ExecuteAsync(
                "INSERT INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (@PizzaId, @IngredienteId);",
                new { PizzaId = pizzaId, IngredienteId = ingredienteId }, transaction);
        }
    }
}