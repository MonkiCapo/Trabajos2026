using Api_Institucion.Dtos;
using Api_Institucion.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api_Institucion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CursosController : ControllerBase
{
    private readonly IInstitucionService _service;

    public CursosController(IInstitucionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta el catálogo de cursos con filtros opcionales por estado o término de búsqueda.
    /// Caso de uso: Estudiante (Buscar cursos), Docente, Administrador.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CursoResponseDto>), StatusCodes.Status200OK)]
    public IActionResult ObtenerCursos([FromQuery] string? estado, [FromQuery] string? buscar)
    {
        var cursos = _service.ObtenerCursos(estado, buscar);
        return Ok(cursos);
    }

    /// <summary>
    /// Consulta el detalle de un curso específico por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CursoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ObtenerPorId(int id)
    {
        var curso = _service.ObtenerCursoPorId(id);
        if (curso is null)
        {
            return NotFound(new { mensaje = $"No se encontró el curso con ID {id}." });
        }
        return Ok(curso);
    }

    /// <summary>
    /// Permite al Administrador dar de alta un nuevo curso académico (RF5).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CursoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CrearCurso([FromBody] CrearCursoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new { mensaje = "El nombre del curso es obligatorio." });
        }

        if (dto.CupoMaximo <= 0)
        {
            return BadRequest(new { mensaje = "El cupo máximo debe ser mayor a 0." });
        }

        var nuevoCurso = _service.CrearCurso(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoCurso.IdCurso }, nuevoCurso);
    }

    /// <summary>
    /// Permite al Administrador asignar un docente a un curso específico (RF4).
    /// </summary>
    [HttpPatch("{id:int}/docente")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult AsignarDocente(int id, [FromBody] AsignarDocenteDto dto)
    {
        var (exito, error) = _service.AsignarDocenteACurso(id, dto.LegajoDocente);
        if (!exito)
        {
            return BadRequest(new { mensaje = error });
        }

        return Ok(new { mensaje = $"Docente '{dto.LegajoDocente}' asignado correctamente al curso {id}." });
    }

    /// <summary>
    /// Permite al Administrador abrir o cerrar las inscripciones a un curso.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CambiarEstado(int id, [FromBody] CambiarEstadoCursoDto dto)
    {
        var (exito, error) = _service.CambiarEstadoCurso(id, dto.Estado);
        if (!exito)
        {
            return BadRequest(new { mensaje = error });
        }

        return Ok(new { mensaje = $"Estado del curso {id} actualizado a '{dto.Estado}'." });
    }

    /// <summary>
    /// Permite al Docente y al Administrador consultar los estudiantes inscriptos en un curso.
    /// </summary>
    [HttpGet("{id:int}/alumnos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ObtenerAlumnosInscriptos(int id)
    {
        var alumnos = _service.ObtenerAlumnosPorCurso(id);
        return Ok(alumnos);
    }
}
