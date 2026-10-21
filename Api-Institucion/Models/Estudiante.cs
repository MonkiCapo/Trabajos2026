namespace Api_Institucion.Models;

public class Estudiante
{
    public string Legajo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public Estudiante() { }

    public Estudiante(string legajo, string nombre, string apellido, string email)
    {
        Legajo = legajo;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }
}
