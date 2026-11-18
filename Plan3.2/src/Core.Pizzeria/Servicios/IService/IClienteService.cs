using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IService;

public interface IClienteService
{
    Task<Cliente?> ObtenerClientePorIdAsync(int id);
    Task<Cliente?> ObtenerClientePorEmailAsync(string email);
}
