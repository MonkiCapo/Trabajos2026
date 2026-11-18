using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IPizzaRepositorio
{
    Task<IEnumerable<Pizza>> ObtenerPizzasAsync();
    Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre);
    Task<Pizza?> ObtenerPizzaPorIdAsync(int id);
    Task<IEnumerable<Pizza>> ObtenerPizzasPorNombresAsync(IEnumerable<string> nombres, IDbConnection conexion, IDbTransaction transaction);

    // Ingredientes (por nombre) de las pizzas pedidas, para validar los
    // "quitar" contra la BD dentro de la transaccion del checkout.
    Task<IDictionary<int, List<string>>> ObtenerIngredientesDePizzasAsync(IEnumerable<int> pizzaIds, IDbConnection conexion, IDbTransaction transaction);
    Task<int> InsertarPizzaAsync(Pizza pizza, IDbConnection conexion, IDbTransaction transaction);
    Task ActualizarPizzaAsync(Pizza pizza, IDbConnection conexion, IDbTransaction transaction);
    Task<int> EliminarPizzaAsync(int id);
    Task ReemplazarIngredientesAsync(int pizzaId, IEnumerable<int> ingredienteIds, IDbConnection conexion, IDbTransaction transaction);
}
