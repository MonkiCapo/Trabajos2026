using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IPizzaRepositorio
{
    Task<IEnumerable<Pizza>> ObtenerPizzasAsync(bool soloDisponibles = true, CancellationToken ct = default);
    Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre, CancellationToken ct = default);
    Task<IEnumerable<Pizza>> ObtenerPizzasPorNombresAsync(IEnumerable<string> nombres, CancellationToken ct = default);
    Task<Pizza?> ObtenerPizzaPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistePizzaConNombreAsync(string nombre, int? excluyendoId = null, CancellationToken ct = default);
    Task<int> AgregarPizzaAsync(Pizza pizza, CancellationToken ct = default);
    Task<bool> ActualizarPizzaAsync(Pizza pizza, CancellationToken ct = default);
    Task<bool> EliminarPizzaAsync(int id, CancellationToken ct = default);
    Task<bool> TieneItemsAsociadosAsync(int pizzaId, CancellationToken ct = default);
    Task<bool> DescontarStockAsync(int pizzaId, int cantidad, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default);
    Task<bool> RestaurarStockAsync(int pizzaId, int cantidad, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default);
}