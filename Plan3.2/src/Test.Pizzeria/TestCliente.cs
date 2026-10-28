using Xunit;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Services.Pizzeria.Services;

namespace Test.Pizzeria;



public class TestCliente
{
    Cliente cliente = new Cliente();
    IClienteRepositorio repocliente;

    [Fact]
    public async Task CuandoObtengoLosClientes_DebeRetornarOK()
    {
        cliente.Direccion = "direccion";
        cliente.Email = "email";
        cliente.Nombre = "nombre";
        cliente.Telefono = "telefono";
        var clientes = await repocliente.ObtenerClientesAsync();

        
        
    }
}