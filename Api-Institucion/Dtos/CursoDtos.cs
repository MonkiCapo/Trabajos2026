using Api_Institucion.Models;

namespace Api_Institucion.Dtos;

public record CrearCursoDto(
    string Nombre,
    string Descripcion,
    int CupoMaximo,
    string? LegajoDocente = null
);

public record AsignarDocenteDto(
    string LegajoDocente
);

public record CambiarEstadoCursoDto(
    string Estado // "Abierto" | "Cerrado"
);

public record CursoResponseDto(
    int IdCurso,
    string Nombre,
    string Descripcion,
    int CupoMaximo,
    int CupoDisponible,
    string Estado,
    Docente? Docente
);
