using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IService;

public interface IPizzaService
{
    Task<IEnumerable<Pizza>> ObtenerPizzasAsync();
    Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre);
}
