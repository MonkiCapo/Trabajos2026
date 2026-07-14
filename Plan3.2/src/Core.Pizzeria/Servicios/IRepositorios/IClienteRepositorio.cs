using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IClienteRepositorio
{
    IEnumerable<Cliente> ObtenerClientes();
    Cliente ObtenerClientePorId(int id);
    Cliente ObtenerClientePorEmail(string email);
    Cliente AgregarCliente(Cliente cliente);
    bool ActualizarCliente(Cliente cliente, int id);
    bool EliminarCliente(int id);
    bool ExisteEmailDeCliente(string emailExistente);
}
