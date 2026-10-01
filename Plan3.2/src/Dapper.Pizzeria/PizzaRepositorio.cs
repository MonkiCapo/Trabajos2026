using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Core.Pizzeria.Entidades;
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

        foreach (var pizza in pizzas)
        {
            pizza.Ingredientes = ingredientes
                .Where(ing => ing.PizzaId == pizza.Id)
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
}