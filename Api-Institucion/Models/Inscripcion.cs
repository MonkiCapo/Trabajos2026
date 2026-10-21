namespace Api_Institucion.Models;

public class Inscripcion
{
    public int IdInscripcion { get; set; }
    public string LegajoEstudiante { get; set; } = string.Empty;
    public int IdCurso { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Estado { get; set; } = "Confirmada"; // "Confirmada" | "Cancelada"

    public Inscripcion() { }

    public Inscripcion(int idInscripcion, string legajoEstudiante, int idCurso)
    {
        IdInscripcion = idInscripcion;
        LegajoEstudiante = legajoEstudiante;
        IdCurso = idCurso;
        Fecha = DateTime.UtcNow;
        Estado = "Confirmada";
    }

    public void Cancelar() => Estado = "Cancelada";
}
