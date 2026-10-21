namespace Api_Institucion.Dtos;

public record CrearEstudianteDto(
    string Legajo,
    string Nombre,
    string Apellido,
    string Email
);

public record CrearDocenteDto(
    string Legajo,
    string Nombre,
    string Apellido,
    string Email
);
