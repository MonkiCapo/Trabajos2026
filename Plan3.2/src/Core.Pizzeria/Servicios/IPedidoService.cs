using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Servicios;

public interface IPedidoService
{
    Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido, CancellationToken ct = default);
    Task<Pedido> ModificarPedidoAsync(int pedidoId, List<ItemRequest> nuevosItems, CancellationToken ct = default);
    Task ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, string observacion, CancellationToken ct = default);
    Task CambiarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, CancellationToken ct = default);
    Task<Pedido?> GetPedidoByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Pedido>> GetPedidosAsync(EstadoPedido? estado = null, CancellationToken ct = default);
}