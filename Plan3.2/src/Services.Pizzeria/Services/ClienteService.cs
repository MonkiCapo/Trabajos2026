using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepositorio _repocliente;
    private readonly IValidator<ClienteRequest> _validadorCliente;

    public ClienteService(IClienteRepositorio repocliente, IValidator<ClienteRequest> validadorCliente)
    {
        _repocliente = repocliente;
        _validadorCliente = validadorCliente;
    }

    public async Task<IEnumerable<Cliente>> ObtenerClientesAsync()
    {
        return await _repocliente.ObtenerClientesAsync();
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int id)
    {
        return await _repocliente.ObtenerClientePorIdAsync(id);
    }

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string email)
    {
        return await _repocliente.ObtenerClientePorEmailAsync(email);
    }

    public async Task<Cliente> AgregarClienteAsync(ClienteRequest clienteRequest)
    {
        var validationResult = await _validadorCliente.ValidateAsync(clienteRequest);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var nuevoCliente = new Cliente
        {
            Nombre = clienteRequest.Nombre,
            Email = clienteRequest.Email,
            Telefono = clienteRequest.Telefono,
            Direccion = clienteRequest.Direccion
        };

        var idGenerado = await _repocliente.AgregarClienteAsync(nuevoCliente);
        nuevoCliente.Id = idGenerado;

        return nuevoCliente;
    }
}
