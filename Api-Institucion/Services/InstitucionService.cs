using Api_Institucion.Dtos;
using Api_Institucion.Models;

namespace Api_Institucion.Services;

/// <summary>
/// Implementación pura en memoria (sin base de datos) para la gestión del dominio académico.
/// Diseñada con seguridad contra condiciones de carrera (thread-safe) y datos iniciales didácticos.
/// </summary>
public class InstitucionService : IInstitucionService
{
    private readonly List<Estudiante> _estudiantes = new();
    private readonly List<Docente> _docentes = new();
    private readonly List<Curso> _cursos = new();
    private readonly List<Inscripcion> _inscripciones = new();

    private readonly object _lock = new();
    private int _proximoIdCurso = 4;
    private int _proximoIdInscripcion = 2;

    public InstitucionService()
    {
        CargarDatosSemilla();
    }

    private void CargarDatosSemilla()
    {
        // 1. Docentes iniciales
        _docentes.Add(new Docente("DOC-101", "Carlos", "Pérez", "cperez@institucion.edu.ar"));
        _docentes.Add(new Docente("DOC-102", "María", "González", "mgonzalez@institucion.edu.ar"));

        // 2. Estudiantes iniciales
        _estudiantes.Add(new Estudiante("EST-201", "Juan", "Gómez", "jgomez@institucion.edu.ar"));
        _estudiantes.Add(new Estudiante("EST-202", "Lucía", "Fernández", "lfernandez@institucion.edu.ar"));
        _estudiantes.Add(new Estudiante("EST-203", "Tomás", "Rodríguez", "trodriguez@institucion.edu.ar"));

        // 3. Cursos iniciales
        // Curso 1: Desarrollo C#, cupo máx: 2, disponible: 1 (para probar límite de cupo)
        var curso1 = new Curso(1, "Desarrollo de Sistemas C#", "API REST y buenas prácticas en .NET", 2, "DOC-101");
        curso1.CupoDisponible = 1; // 1 cupo ocupado por EST-201
        _cursos.Add(curso1);

        // Curso 2: Bases de Datos, cupo máx: 20, disponible: 20
        var curso2 = new Curso(2, "Bases de Datos Relacionales", "Modelado relacional y SQL", 20, "DOC-102");
        _cursos.Add(curso2);

        // Curso 3: Arquitectura Web, Cerrado
        var curso3 = new Curso(3, "Arquitectura Web Avanzada", "Microservicios y patrones en la nube", 15, "DOC-101");
        curso3.Cerrar();
        _cursos.Add(curso3);

        // 4. Inscripción existente (EST-201 en Curso 1)
        var inscripcion1 = new Inscripcion(1, "EST-201", 1)
        {
            Fecha = DateTime.UtcNow.AddDays(-1),
            Estado = "Confirmada"
        };
        _inscripciones.Add(inscripcion1);
    }

    #region Cursos

    public IEnumerable<CursoResponseDto> ObtenerCursos(string? estado = null, string? buscar = null)
    {
        lock (_lock)
        {
            var consulta = _cursos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                consulta = consulta.Where(c => c.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                consulta = consulta.Where(c => c.Nombre.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                                               c.Descripcion.Contains(buscar, StringComparison.OrdinalIgnoreCase));
            }

            return consulta.Select(MapearCursoResponse).ToList();
        }
    }

    public CursoResponseDto? ObtenerCursoPorId(int id)
    {
        lock (_lock)
        {
            var curso = _cursos.FirstOrDefault(c => c.IdCurso == id);
            return curso is null ? null : MapearCursoResponse(curso);
        }
    }

    public CursoResponseDto CrearCurso(CrearCursoDto dto)
    {
        lock (_lock)
        {
            var nuevoCurso = new Curso(_proximoIdCurso++, dto.Nombre, dto.Descripcion, dto.CupoMaximo, dto.LegajoDocente);
            _cursos.Add(nuevoCurso);
            return MapearCursoResponse(nuevoCurso);
        }
    }

    public (bool exito, string? error) AsignarDocenteACurso(int idCurso, string legajoDocente)
    {
        lock (_lock)
        {
            var curso = _cursos.FirstOrDefault(c => c.IdCurso == idCurso);
            if (curso is null) return (false, $"No se encontró el curso con ID {idCurso}.");

            var docente = _docentes.FirstOrDefault(d => d.Legajo.Equals(legajoDocente, StringComparison.OrdinalIgnoreCase));
            if (docente is null) return (false, $"No se encontró el docente con legajo '{legajoDocente}'.");

            curso.AsignarDocente(docente.Legajo);
            return (true, null);
        }
    }

    public (bool exito, string? error) CambiarEstadoCurso(int idCurso, string nuevoEstado)
    {
        lock (_lock)
        {
            var curso = _cursos.FirstOrDefault(c => c.IdCurso == idCurso);
            if (curso is null) return (false, $"No se encontró el curso con ID {idCurso}.");

            if (nuevoEstado.Equals("Abierto", StringComparison.OrdinalIgnoreCase))
            {
                curso.Habilitar();
                return (true, null);
            }

            if (nuevoEstado.Equals("Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                curso.Cerrar();
                return (true, null);
            }

            return (false, "El estado proporcionado no es válido. Debe ser 'Abierto' o 'Cerrado'.");
        }
    }

    public IEnumerable<Estudiante> ObtenerAlumnosPorCurso(int idCurso)
    {
        lock (_lock)
        {
            var legajosInscriptos = _inscripciones
                .Where(i => i.IdCurso == idCurso && i.Estado == "Confirmada")
                .Select(i => i.LegajoEstudiante)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return _estudiantes.Where(e => legajosInscriptos.Contains(e.Legajo)).ToList();
        }
    }

    #endregion

    #region Inscripciones

    public (InscripcionResponseDto? inscripcion, string? error, int statusCode) InscribirEstudiante(string legajoEstudiante, int idCurso)
    {
        lock (_lock)
        {
            // 1. Validar que el estudiante exista
            var estudiante = _estudiantes.FirstOrDefault(e => e.Legajo.Equals(legajoEstudiante, StringComparison.OrdinalIgnoreCase));
            if (estudiante is null)
            {
                return (null, $"No existe ningún estudiante registrado con el legajo '{legajoEstudiante}'.", 404);
            }

            // 2. Validar que el curso exista
            var curso = _cursos.FirstOrDefault(c => c.IdCurso == idCurso);
            if (curso is null)
            {
                return (null, $"No existe el curso con ID {idCurso}.", 404);
            }

            // 3. Validar estado del curso (debe estar abierto)
            if (!curso.Estado.Equals("Abierto", StringComparison.OrdinalIgnoreCase))
            {
                return (null, $"El curso '{curso.Nombre}' se encuentra CERRADO para nuevas inscripciones.", 400);
            }

            // 4. Validar si ya está inscripto
            bool yaInscripto = _inscripciones.Any(i =>
                i.IdCurso == idCurso &&
                i.LegajoEstudiante.Equals(legajoEstudiante, StringComparison.OrdinalIgnoreCase) &&
                i.Estado == "Confirmada");

            if (yaInscripto)
            {
                return (null, $"El estudiante {estudiante.Nombre} {estudiante.Apellido} ya se encuentra inscripto en este curso.", 400);
            }

            // 5. INCLUDE: Verificar si hay cupo disponible
            if (!curso.TieneCupo())
            {
                return (null, $"No hay cupo disponible para el curso '{curso.Nombre}'. Cupo máximo alcanzado ({curso.CupoMaximo}).", 409); // 409 Conflict
            }

            // 6. Aplicar descuento de cupo y crear inscripción
            curso.DescontarCupo();
            var nuevaInscripcion = new Inscripcion(_proximoIdInscripcion++, estudiante.Legajo, curso.IdCurso);
            _inscripciones.Add(nuevaInscripcion);

            var responseDto = new InscripcionResponseDto(
                nuevaInscripcion.IdInscripcion,
                nuevaInscripcion.Fecha,
                nuevaInscripcion.Estado,
                estudiante,
                curso
            );

            return (responseDto, null, 201);
        }
    }

    public (bool exito, string? error) CancelarInscripcion(int idInscripcion)
    {
        lock (_lock)
        {
            var inscripcion = _inscripciones.FirstOrDefault(i => i.IdInscripcion == idInscripcion);
            if (inscripcion is null)
            {
                return (false, $"No se encontró la inscripción #{idInscripcion}.");
            }

            if (inscripcion.Estado == "Cancelada")
            {
                return (false, "La inscripción ya se encontraba cancelada.");
            }

            inscripcion.Cancelar();

            // Liberar cupo en el curso asociado
            var curso = _cursos.FirstOrDefault(c => c.IdCurso == inscripcion.IdCurso);
            curso?.LiberarCupo();

            return (true, null);
        }
    }

    public InscripcionResponseDto? ObtenerInscripcionPorId(int id)
    {
        lock (_lock)
        {
            var inscripcion = _inscripciones.FirstOrDefault(i => i.IdInscripcion == id);
            return inscripcion is null ? null : MapearInscripcionResponse(inscripcion);
        }
    }

    public IEnumerable<InscripcionResponseDto> ObtenerInscripcionesPorEstudiante(string legajo)
    {
        lock (_lock)
        {
            return _inscripciones
                .Where(i => i.LegajoEstudiante.Equals(legajo, StringComparison.OrdinalIgnoreCase))
                .Select(MapearInscripcionResponse)
                .ToList();
        }
    }

    #endregion

    #region Docentes y Estudiantes

    public IEnumerable<CursoResponseDto> ObtenerCursosPorDocente(string legajoDocente)
    {
        lock (_lock)
        {
            return _cursos
                .Where(c => c.LegajoDocente != null && c.LegajoDocente.Equals(legajoDocente, StringComparison.OrdinalIgnoreCase))
                .Select(MapearCursoResponse)
                .ToList();
        }
    }

    public IEnumerable<Estudiante> ObtenerEstudiantes()
    {
        lock (_lock)
        {
            return _estudiantes.ToList();
        }
    }

    public Estudiante? ObtenerEstudiantePorLegajo(string legajo)
    {
        lock (_lock)
        {
            return _estudiantes.FirstOrDefault(e => e.Legajo.Equals(legajo, StringComparison.OrdinalIgnoreCase));
        }
    }

    public (Estudiante? estudiante, string? error) RegistrarEstudiante(CrearEstudianteDto dto)
    {
        lock (_lock)
        {
            if (_estudiantes.Any(e => e.Legajo.Equals(dto.Legajo, StringComparison.OrdinalIgnoreCase)))
            {
                return (null, $"Ya existe un estudiante con el legajo '{dto.Legajo}'.");
            }

            var nuevo = new Estudiante(dto.Legajo, dto.Nombre, dto.Apellido, dto.Email);
            _estudiantes.Add(nuevo);
            return (nuevo, null);
        }
    }

    public IEnumerable<Docente> ObtenerDocentes()
    {
        lock (_lock)
        {
            return _docentes.ToList();
        }
    }

    public Docente? ObtenerDocentePorLegajo(string legajo)
    {
        lock (_lock)
        {
            return _docentes.FirstOrDefault(d => d.Legajo.Equals(legajo, StringComparison.OrdinalIgnoreCase));
        }
    }

    public (Docente? docente, string? error) RegistrarDocente(CrearDocenteDto dto)
    {
        lock (_lock)
        {
            if (_docentes.Any(d => d.Legajo.Equals(dto.Legajo, StringComparison.OrdinalIgnoreCase)))
            {
                return (null, $"Ya existe un docente con el legajo '{dto.Legajo}'.");
            }

            var nuevo = new Docente(dto.Legajo, dto.Nombre, dto.Apellido, dto.Email);
            _docentes.Add(nuevo);
            return (nuevo, null);
        }
    }

    #endregion

    #region Mapeos Auxiliares

    private CursoResponseDto MapearCursoResponse(Curso curso)
    {
        Docente? docente = null;
        if (!string.IsNullOrEmpty(curso.LegajoDocente))
        {
            docente = _docentes.FirstOrDefault(d => d.Legajo.Equals(curso.LegajoDocente, StringComparison.OrdinalIgnoreCase));
        }

        return new CursoResponseDto(
            curso.IdCurso,
            curso.Nombre,
            curso.Descripcion,
            curso.CupoMaximo,
            curso.CupoDisponible,
            curso.Estado,
            docente
        );
    }

    private InscripcionResponseDto MapearInscripcionResponse(Inscripcion inscripcion)
    {
        var estudiante = _estudiantes.FirstOrDefault(e => e.Legajo.Equals(inscripcion.LegajoEstudiante, StringComparison.OrdinalIgnoreCase));
        var curso = _cursos.FirstOrDefault(c => c.IdCurso == inscripcion.IdCurso);

        return new InscripcionResponseDto(
            inscripcion.IdInscripcion,
            inscripcion.Fecha,
            inscripcion.Estado,
            estudiante,
            curso
        );
    }

    #endregion
}
