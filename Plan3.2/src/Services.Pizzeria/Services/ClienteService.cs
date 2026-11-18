using System.Threading.Tasks;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepositorio _repocliente;

    public ClienteService(IClienteRepositorio repocliente)
    {
        _repocliente = repocliente;
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int id)
    {
        return await _repocliente.ObtenerClientePorIdAsync(id);
    }

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string email)
    {
        return await _repocliente.ObtenerClientePorEmailAsync(email);
    }
}
