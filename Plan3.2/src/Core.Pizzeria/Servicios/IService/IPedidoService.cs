using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios;

public interface IPedidoService
{
    Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido);

    /// <summary>
    /// Cierra un pedido a partir de los datos de contacto y las pizzas elegidas.
    /// Si el cliente no existe todavia, lo da de alta dentro de la misma transaccion.
    /// </summary>
    Task<Pedido> CrearPedidoConDatosAsync(CheckoutRequest request);

    Task ActualizarEstadoAsync(int pedidoId, Servicios.Enum.EstadoPedido nuevoEstado, string observacion);
    Task<Pedido?> GetPedidoByIdAsync(int id);

    /// <summary>
    /// Historial de cambios de estado del pedido, del mas antiguo al mas nuevo.
    /// Devuelve una lista vacia si el pedido todavia no cambio de estado.
    /// </summary>
    Task<IEnumerable<HistorialEstadoPedido>> ObtenerHistorialAsync(int pedidoId);
}
