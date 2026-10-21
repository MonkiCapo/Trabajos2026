namespace Api_Institucion.Models;

public class Docente
{
    public string Legajo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public Docente() { }

    public Docente(string legajo, string nombre, string apellido, string email)
    {
        Legajo = legajo;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }
}
