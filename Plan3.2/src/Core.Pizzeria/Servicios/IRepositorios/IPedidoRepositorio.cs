using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IPedidoRepositorio
{
    Pedido AgregarPedido(Pedido pedido);
    bool ActualizarEstado(int pedidoId, Servicios.Enum.EstadoPedido nuevoEstado);
    Pedido ObtenerPedidoPorId(int id);
    IEnumerable<Pedido> ObtenerPedidos();
}
