using System;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Servicios.Excepciones;

public class PedidoNoModificableException : Exception
{
    public EstadoPedido EstadoActual { get; }

    public PedidoNoModificableException(int pedidoId, EstadoPedido estadoActual)
        : base($"El pedido {pedidoId} no puede modificarse porque su estado es '{estadoActual}'. " +
               "Solo los pedidos en estado 'EsperaConfirmacion' pueden modificarse.")
    {
        EstadoActual = estadoActual;
    }
}
