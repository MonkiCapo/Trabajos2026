namespace Api_Institucion.Models;

public class Curso
{
    public int IdCurso { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int CupoMaximo { get; set; }
    public int CupoDisponible { get; set; }
    public string Estado { get; set; } = "Abierto"; // "Abierto" | "Cerrado"
    public string? LegajoDocente { get; set; }

    public Curso() { }

    public Curso(int idCurso, string nombre, string descripcion, int cupoMaximo, string? legajoDocente = null)
    {
        IdCurso = idCurso;
        Nombre = nombre;
        Descripcion = descripcion;
        CupoMaximo = cupoMaximo;
        CupoDisponible = cupoMaximo;
        Estado = "Abierto";
        LegajoDocente = legajoDocente;
    }

    // Métodos de comportamiento/responsabilidades del dominio (según apunte)
    public void Habilitar() => Estado = "Abierto";
    
    public void Cerrar() => Estado = "Cerrado";
    
    public bool TieneCupo() => CupoDisponible > 0;
    
    public bool DescontarCupo()
    {
        if (!TieneCupo()) return false;
        CupoDisponible--;
        return true;
    }

    public void LiberarCupo()
    {
        if (CupoDisponible < CupoMaximo)
        {
            CupoDisponible++;
        }
    }

    public void AsignarDocente(string legajo)
    {
        LegajoDocente = legajo;
    }
}
