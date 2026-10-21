using Api_Institucion.Models;

namespace Api_Institucion.Dtos;

public record CrearInscripcionDto(
    string LegajoEstudiante,
    int IdCurso
);

public record InscripcionResponseDto(
    int IdInscripcion,
    DateTime Fecha,
    string Estado,
    Estudiante? Estudiante,
    Curso? Curso
);
