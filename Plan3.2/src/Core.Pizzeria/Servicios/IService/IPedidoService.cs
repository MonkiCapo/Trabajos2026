using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios;

public interface IPedidoService
{
    Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido);

    Task<Pedido> CrearPedidoConDatosAsync(CheckoutRequest request);

    Task ActualizarEstadoAsync(int pedidoId, Servicios.Enum.EstadoPedido nuevoEstado, string observacion);
    Task<Pedido?> GetPedidoByIdAsync(int id);

    Task<IEnumerable<HistorialEstadoPedido>> ObtenerHistorialAsync(int pedidoId);
}
