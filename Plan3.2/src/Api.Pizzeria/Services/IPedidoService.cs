using System.Threading.Tasks;
using Api.Pizzeria.Models;

namespace Api.Pizzeria.Services;

public interface IPedidoService
{
    Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido);
    Task ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, string observacion);
    Task<Pedido?> GetPedidoByIdAsync(int id);
}
