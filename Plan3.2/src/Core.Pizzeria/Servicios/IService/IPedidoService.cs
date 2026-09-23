using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios;

public interface IPedidoService
{
    Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido);
    Task ActualizarEstadoAsync(int pedidoId, Servicios.Enum.EstadoPedido nuevoEstado, string observacion);
    Task<Pedido?> GetPedidoByIdAsync(int id);
}
