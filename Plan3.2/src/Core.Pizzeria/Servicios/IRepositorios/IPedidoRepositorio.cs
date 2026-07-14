using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IPedidoRepositorio
{
    Task<int> CrearPedidoAsync(Pedido pedido, IDbConnection conexion, IDbTransaction transaction);
    Task CrearItemsPedidoAsync(List<ItemPedido> items, int pedidoId, IDbConnection conexion, IDbTransaction transaction);
    Task CrearHistorialAsync(int pedidoId, EstadoPedido estado, string observacion, IDbConnection conexion, IDbTransaction transaction);
    Task<bool> ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, IDbConnection conexion, IDbTransaction transaction);
    Task<Pedido?> ObtenerPedidoPorIdAsync(int id);
    Task<IEnumerable<Pedido>> ObtenerPedidosAsync();
}
