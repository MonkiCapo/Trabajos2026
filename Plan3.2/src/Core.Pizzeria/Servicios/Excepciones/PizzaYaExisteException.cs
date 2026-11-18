namespace Core.Pizzeria.Servicios.Excepciones;

// El nombre de pizza choco con el UNIQUE de la BD (carrera entre el chequeo
// previo y el INSERT): el servicio lo traduce a un error de validacion 400.
public class PizzaYaExisteException : Exception
{
    public PizzaYaExisteException(string mensaje, Exception interna) : base(mensaje, interna)
    {
    }
}
