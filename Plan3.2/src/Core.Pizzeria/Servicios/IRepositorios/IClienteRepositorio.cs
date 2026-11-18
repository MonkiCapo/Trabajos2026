using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IClienteRepositorio
{
    Task<IEnumerable<Cliente>> ObtenerClientesAsync(CancellationToken ct = default);
    Task<Cliente?> ObtenerClientePorIdAsync(int id, CancellationToken ct = default);
    Task<Cliente?> ObtenerClientePorEmailAsync(string email, CancellationToken ct = default);
    Task<int> AgregarClienteAsync(Cliente cliente, CancellationToken ct = default);
    Task<bool> ActualizarClienteAsync(Cliente cliente, int id, CancellationToken ct = default);
    Task<bool> EliminarClienteAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteEmailDeClienteAsync(string emailExistente, CancellationToken ct = default);
}