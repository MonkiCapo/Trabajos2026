using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class PizzaService : IPizzaService
{
    private readonly IPizzaRepositorio _pizzaRepo;

    public PizzaService(IPizzaRepositorio pizzaRepo)
    {
        _pizzaRepo = pizzaRepo;
    }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasAsync()
    {
        return await _pizzaRepo.ObtenerPizzasAsync();
    }

    public async Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre)
    {
        return await _pizzaRepo.ObtenerPizzaPorNombreAsync(nombre);
    }
}
