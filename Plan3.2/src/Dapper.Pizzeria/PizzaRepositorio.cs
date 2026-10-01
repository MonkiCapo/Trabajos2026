using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class PizzaRepositorio : DapperRepo, IPizzaRepositorio
{
    public PizzaRepositorio(IAdo ado) : base(ado) { }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasAsync()
    {
        var sql = "SELECT id, nombre, tamanio, precio FROM PIZZA;";

        using var conexion = NuevaConexion();

        return await conexion.QueryAsync<Pizza>(sql);
    }

    public async Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre)
    {
        var sql = "SELECT id, nombre, tamanio, precio FROM PIZZA WHERE nombre = @Nombre;";

        using var conexion = NuevaConexion();

        return await conexion.QueryFirstOrDefaultAsync<Pizza>(sql, new { Nombre = nombre });
    }
}
