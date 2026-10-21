using Api_Institucion.Dtos;
using Api_Institucion.Models;

namespace Api_Institucion.Services;

public interface IInstitucionService
{
    // Cursos
    IEnumerable<CursoResponseDto> ObtenerCursos(string? estado = null, string? buscar = null);
    CursoResponseDto? ObtenerCursoPorId(int id);
    CursoResponseDto CrearCurso(CrearCursoDto dto);
    (bool exito, string? error) AsignarDocenteACurso(int idCurso, string legajoDocente);
    (bool exito, string? error) CambiarEstadoCurso(int idCurso, string nuevoEstado);
    IEnumerable<Estudiante> ObtenerAlumnosPorCurso(int idCurso);

    // Inscripciones
    (InscripcionResponseDto? inscripcion, string? error, int statusCode) InscribirEstudiante(string legajoEstudiante, int idCurso);
    (bool exito, string? error) CancelarInscripcion(int idInscripcion);
    InscripcionResponseDto? ObtenerInscripcionPorId(int id);
    IEnumerable<InscripcionResponseDto> ObtenerInscripcionesPorEstudiante(string legajo);

    // Docentes & Estudiantes
    IEnumerable<CursoResponseDto> ObtenerCursosPorDocente(string legajoDocente);
    IEnumerable<Estudiante> ObtenerEstudiantes();
    Estudiante? ObtenerEstudiantePorLegajo(string legajo);
    (Estudiante? estudiante, string? error) RegistrarEstudiante(CrearEstudianteDto dto);

    IEnumerable<Docente> ObtenerDocentes();
    Docente? ObtenerDocentePorLegajo(string legajo);
    (Docente? docente, string? error) RegistrarDocente(CrearDocenteDto dto);
}
