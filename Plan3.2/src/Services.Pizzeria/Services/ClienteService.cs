using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;
using Services.Pizzeria.Validations;

namespace Services.Pizzeria.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepositorio _repocliente;
    private readonly ClienteRequestValidator _validadorcliente;

    public ClienteService(IClienteRepositorio repocliente, ClienteRequestValidator validadorcliente)
    {
        _repocliente = repocliente;
        _validadorcliente = validadorcliente;
    }


    public async Task<IEnumerable<ClienteRequest>> ObtenerClientesAsync()
    {
        var clientes = await _repocliente.ObtenerClientesAsync();
        
        return clientes.Select(cliente => new ClienteRequest
        {
            Nombre = cliente.Nombre,
            Email = cliente.Email,
            Telefono = cliente.Telefono,
            Direccion = cliente.Direccion
        });   
    }

    public async Task<ClienteRequest> ObtenerClienteporEmailAsync(string email)
    {
        var cliente = await _repocliente.ObtenerClientePorEmailAsync(email);
        return cliente is null ? null! : new ClienteRequest
        {
            Nombre = cliente.Nombre,
            Email = cliente.Email,
            Telefono = cliente.Telefono,
            Direccion = cliente.Direccion
        };
    }

    public Task<ClienteRequest> AgregarClienteAsync(ClienteRequest clienteRequest)
    {
        
    }

}
