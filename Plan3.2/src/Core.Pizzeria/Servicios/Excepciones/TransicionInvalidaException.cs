using System;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Servicios.Excepciones;

public class TransicionInvalidaException : Exception
{
    public EstadoPedido EstadoActual { get; }
    public EstadoPedido EstadoSolicitado { get; }

    public TransicionInvalidaException(EstadoPedido estadoActual, EstadoPedido estadoSolicitado)
        : base($"Transición inválida: no se puede pasar de '{estadoActual}' a '{estadoSolicitado}'. " +
               "El ciclo válido es EsperaConfirmacion -> EnPreparacion -> EnViaje -> Entregado.")
    {
        EstadoActual = estadoActual;
        EstadoSolicitado = estadoSolicitado;
    }
}