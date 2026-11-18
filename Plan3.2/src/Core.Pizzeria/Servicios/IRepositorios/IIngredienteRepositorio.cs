using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IIngredienteRepositorio
{
    Task<IEnumerable<Ingrediente>> ObtenerIngredientesAsync();
    Task<IEnumerable<Ingrediente>> ObtenerPorNombresAsync(IEnumerable<string> nombres, IDbConnection conexion, IDbTransaction transaction);
}
