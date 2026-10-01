using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IClienteRepositorio
{
    Task<IEnumerable<Cliente>> ObtenerClientesAsync();
    Task<Cliente?> ObtenerClientePorIdAsync(int id);
    Task<Cliente?> ObtenerClientePorEmailAsync(string email, IDbConnection? conexion = null, IDbTransaction? transaction = null);
    Task<int> AgregarClienteAsync(Cliente cliente, IDbConnection? conexion = null, IDbTransaction? transaction = null);
    Task<bool> ActualizarClienteAsync(Cliente cliente, int id);
    Task<bool> EliminarClienteAsync(int id);
    Task<bool> ExisteEmailDeClienteAsync(string emailExistente);
}
