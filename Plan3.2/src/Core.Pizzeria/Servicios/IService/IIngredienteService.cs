using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IService;

public interface IIngredienteService
{
    Task<IEnumerable<Ingrediente>> ObtenerIngredientesAsync();
}
