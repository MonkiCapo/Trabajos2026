using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class IngredienteService : IIngredienteService
{
    private readonly IIngredienteRepositorio _ingredienteRepo;

    public IngredienteService(IIngredienteRepositorio ingredienteRepo)
    {
        _ingredienteRepo = ingredienteRepo;
    }

    public async Task<IEnumerable<Ingrediente>> ObtenerIngredientesAsync()
    {
        return await _ingredienteRepo.ObtenerIngredientesAsync();
    }
}
