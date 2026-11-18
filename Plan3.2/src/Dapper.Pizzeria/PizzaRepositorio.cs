using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class PizzaRepositorio : DapperRepo, IPizzaRepositorio
{
    private const string Columnas = "id, nombre, tamanio, precio, descripcion, stock, disponible";

    public PizzaRepositorio(IAdo ado) : base(ado) { }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasAsync(bool soloDisponibles = true, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM PIZZA";
        if (soloDisponibles)
        {
            sql += " WHERE disponible = 1";
        }
        sql += " ORDER BY id;";
        return await conexion.QueryAsync<Pizza>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM PIZZA WHERE nombre = @Nombre;";
        return await conexion.QueryFirstOrDefaultAsync<Pizza>(new CommandDefinition(sql, new { Nombre = nombre }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasPorNombresAsync(IEnumerable<string> nombres, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM PIZZA WHERE nombre IN @Nombres;";
        return await conexion.QueryAsync<Pizza>(new CommandDefinition(sql, new { Nombres = nombres }, cancellationToken: ct));
    }

    public async Task<Pizza?> ObtenerPizzaPorIdAsync(int id, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM PIZZA WHERE id = @Id;";
        return await conexion.QueryFirstOrDefaultAsync<Pizza>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<bool> ExistePizzaConNombreAsync(string nombre, int? excluyendoId = null, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = "SELECT COUNT(1) FROM PIZZA WHERE nombre = @Nombre";
        if (excluyendoId.HasValue)
        {
            sql += " AND id <> @Id";
        }
        sql += ";";
        var count = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Nombre = nombre, Id = excluyendoId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<int> AgregarPizzaAsync(Pizza pizza, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        const string sql = @"INSERT INTO PIZZA (nombre, tamanio, precio, descripcion, stock, disponible)
                             VALUES (@Nombre, @Tamanio, @Precio, @Descripcion, @Stock, @Disponible);
                             SELECT LAST_INSERT_ID();";
        return await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, pizza, cancellationToken: ct));
    }

    public async Task<bool> ActualizarPizzaAsync(Pizza pizza, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        const string sql = @"UPDATE PIZZA
                             SET nombre = @Nombre, tamanio = @Tamanio, precio = @Precio,
                                 descripcion = @Descripcion, stock = @Stock, disponible = @Disponible
                             WHERE id = @Id;";
        var rows = await conexion.ExecuteAsync(new CommandDefinition(sql, pizza, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> EliminarPizzaAsync(int id, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        const string sql = "DELETE FROM PIZZA WHERE id = @Id;";
        var rows = await conexion.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> TieneItemsAsociadosAsync(int pizzaId, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM ITEM_PEDIDO WHERE pizza_id = @Id;";
        var count = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Id = pizzaId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<bool> DescontarStockAsync(int pizzaId, int cantidad, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default)
    {
        const string sql = "UPDATE PIZZA SET stock = stock - @Cantidad WHERE id = @Id AND stock >= @Cantidad;";
        var rows = await conexion.ExecuteAsync(new CommandDefinition(sql, new { Id = pizzaId, Cantidad = cantidad }, transaction, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> RestaurarStockAsync(int pizzaId, int cantidad, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default)
    {
        const string sql = "UPDATE PIZZA SET stock = stock + @Cantidad WHERE id = @Id;";
        var rows = await conexion.ExecuteAsync(new CommandDefinition(sql, new { Id = pizzaId, Cantidad = cantidad }, transaction, cancellationToken: ct));
        return rows > 0;
    }
}