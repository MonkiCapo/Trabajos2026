using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Servicios;

public interface IPedidoService
{
    Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido);

    Task<Pedido> CrearPedidoConDatosAsync(CheckoutRequest request);

    Task ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, string observacion);
    Task<Pedido?> GetPedidoByIdAsync(int id);

    Task<IEnumerable<HistorialEstadoPedido>> ObtenerHistorialAsync(int pedidoId);
}
