using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IService;

public interface IPizzaService
{
    Task<IEnumerable<Pizza>> ObtenerPizzasAsync();
    Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre);
    Task<Pizza> CrearPizzaAsync(PizzaRequest request);
    Task<Pizza?> ActualizarPizzaAsync(int id, PizzaRequest request);
    Task<bool> EliminarPizzaAsync(int id);
}
