namespace Core.Pizzeria.Servicios.Excepciones;

// ITEM_PEDIDO referencia a la pizza (FK RESTRICT): no se puede eliminar.
// El endpoint lo traduce a 409 Conflict.
public class PizzaEnUsoException : Exception
{
    public PizzaEnUsoException(string mensaje, Exception interna) : base(mensaje, interna)
    {
    }
}
