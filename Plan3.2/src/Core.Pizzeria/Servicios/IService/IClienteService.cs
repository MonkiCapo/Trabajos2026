using Core.Pizzeria.Entidades;
using Core.Pizzeria.DTOs;
using System.Threading.Tasks;

namespace Core.Pizzeria.Servicios.IService
{
    public interface IClienteService
    {
        Task<IEnumerable<ClienteRequest>> ObtenerClientesAsync();
        Task<ClienteRequest> ObtenerClienteporEmailAsync(string email);
        Task<ClienteRequest> AgregarClienteAsync(ClienteRequest clienteRequest);
    }
}