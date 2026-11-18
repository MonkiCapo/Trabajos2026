using System;

namespace Core.Pizzeria.Servicios.Excepciones;

public class PedidoNoEncontradoException : Exception
{
    public int PedidoId { get; }

    public PedidoNoEncontradoException(int pedidoId)
        : base($"No se encontró el pedido con ID {pedidoId}.")
    {
        PedidoId = pedidoId;
    }
}