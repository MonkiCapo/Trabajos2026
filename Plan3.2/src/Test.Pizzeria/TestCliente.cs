using FluentValidation;
using NSubstitute;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Services.Pizzeria.Services;
using Services.Pizzeria.Validations;

namespace Test.Pizzeria;

public class TestClienteNSubstitute
{
    private readonly IClienteRepositorio _repo = Substitute.For<IClienteRepositorio>();
    private readonly ClienteService _servicio;

    public TestClienteNSubstitute()
    {
        _servicio = new ClienteService(_repo, new ClienteRequestValidator());
    }

    [Fact]
    public async Task ObtenerClientes_DebeRetornarLosClientes()
    {
        var cliente = new Cliente { Nombre = "Ana Perez" };
        _repo.ObtenerClientesAsync().Returns(new List<Cliente> { cliente });

        var resultado = await _servicio.ObtenerClientesAsync();

        Assert.Same(cliente, Assert.Single(resultado));
    }

    [Fact]
    public async Task ObtenerClientePorId_DebeRetornarElCliente()
    {
        var cliente = new Cliente { Id = 7 };
        _repo.ObtenerClientePorIdAsync(7).Returns(cliente);

        var resultado = await _servicio.ObtenerClientePorIdAsync(7);

        Assert.Same(cliente, resultado);
    }

    [Fact]
    public async Task ObtenerClientePorEmail_DebeRetornarElCliente()
    {
        var cliente = new Cliente { Email = "ana@example.com" };
        _repo.ObtenerClientePorEmailAsync("ana@example.com").Returns(cliente);

        var resultado = await _servicio.ObtenerClientePorEmailAsync("ana@example.com");

        Assert.Same(cliente, resultado);
    }

    [Fact]
    public async Task AgregarCliente_DebeAsignarIdYGuardar()
    {
        _repo.AgregarClienteAsync(Arg.Any<Cliente>()).Returns(23);
        var solicitud = new ClienteRequest
        {
            Nombre = "Ana Perez",
            Email = "ana@example.com",
            Telefono = "+54 11 1234-5678",
            Direccion = "Calle 123"
        };

        var creado = await _servicio.AgregarClienteAsync(solicitud);

        Assert.Equal(23, creado.Id);
        Assert.Equal("ana@example.com", creado.Email);
        await _repo.Received(1).AgregarClienteAsync(Arg.Any<Cliente>());
    }
}