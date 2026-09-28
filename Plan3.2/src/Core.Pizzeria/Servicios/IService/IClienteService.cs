using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IService;

public interface IClienteService
{
    Task<IEnumerable<Cliente>> ObtenerClientesAsync();
    Task<Cliente?> ObtenerClientePorIdAsync(int id);
    Task<Cliente?> ObtenerClientePorEmailAsync(string email);
    Task<Cliente> AgregarClienteAsync(ClienteRequest clienteRequest);
}