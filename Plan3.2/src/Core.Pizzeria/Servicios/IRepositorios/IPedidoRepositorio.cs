using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IPedidoRepositorio
{
    Task<int> CrearPedidoAsync(Pedido pedido, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default);
    Task CrearItemsPedidoAsync(List<ItemPedido> items, int pedidoId, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default);
    Task CrearHistorialAsync(int pedidoId, EstadoPedido estado, string observacion, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default);
    Task<bool> ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default);
    Task<Pedido?> ObtenerPedidoPorIdAsync(int id, CancellationToken ct = default);
    Task<EstadoPedido?> ObtenerEstadoActualAsync(int pedidoId, CancellationToken ct = default);
    Task<IEnumerable<Pedido>> ObtenerPedidosAsync(EstadoPedido? estado = null, CancellationToken ct = default);
}