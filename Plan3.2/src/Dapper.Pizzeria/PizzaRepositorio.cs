using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class PizzaRepositorio : DapperRepo, IPizzaRepositorio
{
    public PizzaRepositorio(IAdo _ado) : base(_ado) { }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasAsync()
    {
        var sql = "SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA;";
        return await Conexion.QueryAsync<Pizza>(sql);
    }

    public async Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre)
    {
        var sql = "SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA WHERE nombre = @Nombre;";
        return await Conexion.QueryFirstOrDefaultAsync<Pizza>(sql, new { Nombre = nombre });
    }
}
