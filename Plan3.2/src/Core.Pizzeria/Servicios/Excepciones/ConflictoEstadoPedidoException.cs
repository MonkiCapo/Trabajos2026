namespace Core.Pizzeria.Servicios.Excepciones;

// Otra operacion cambio el estado del pedido entre la lectura y el UPDATE
// condicional: el endpoint lo traduce a 409 Conflict.
public class ConflictoEstadoPedidoException : Exception
{
    public ConflictoEstadoPedidoException(string mensaje) : base(mensaje)
    {
    }
}
