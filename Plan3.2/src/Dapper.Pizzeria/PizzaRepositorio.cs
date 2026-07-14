using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class PizzaRepositorio : DapperRepo, IPizzaRepositorio
{
    public PizzaRepositorio(IAdo _ado) : base(_ado) { }

    public IEnumerable<Pizza> ObtenerPizzas()
    {
        var sql = "SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA;";
        return Conexion.Query<Pizza>(sql);
    }

    public Pizza ObtenerPizzaPorNombre(string nombre)
    {
        var sql = "SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA WHERE nombre = @Nombre;";
        return Conexion.QueryFirstOrDefault<Pizza>(sql, new { Nombre = nombre });
    }
}
