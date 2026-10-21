using Api_Institucion.Dtos;
using Api_Institucion.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api_Institucion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InscripcionesController : ControllerBase
{
    private readonly IInstitucionService _service;

    public InscripcionesController(IInstitucionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registra una nueva inscripción de un estudiante a un curso (RF2).
    /// Ejecuta el caso de uso con la inclusión obligatoria (<<include>>) de:
    /// 1. Verificación de existencia de estudiante y curso.
    /// 2. Verificación de estado del curso ("Abierto").
    /// 3. Verificación de inscripción previa (no duplicada).
    /// 4. Verificación de cupo disponible (retorna 409 Conflict si no hay cupo).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InscripcionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult CrearInscripcion([FromBody] CrearInscripcionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LegajoEstudiante))
        {
            return BadRequest(new { mensaje = "El legajo del estudiante es obligatorio." });
        }

        if (dto.IdCurso <= 0)
        {
            return BadRequest(new { mensaje = "El identificador del curso debe ser un entero positivo." });
        }

        var (inscripcion, error, statusCode) = _service.InscribirEstudiante(dto.LegajoEstudiante, dto.IdCurso);

        if (inscripcion is null)
        {
            return StatusCode(statusCode, new { mensaje = error });
        }

        return CreatedAtAction(nameof(ObtenerPorId), new { id = inscripcion.IdInscripcion }, inscripcion);
    }

    /// <summary>
    /// Consulta el comprobante o detalle de una inscripción por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InscripcionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ObtenerPorId(int id)
    {
        var inscripcion = _service.ObtenerInscripcionPorId(id);
        if (inscripcion is null)
        {
            return NotFound(new { mensaje = $"No se encontró la inscripción #{id}." });
        }
        return Ok(inscripcion);
    }

    /// <summary>
    /// Cancela una inscripción existente y restituye el cupo disponible al curso.
    /// </summary>
    [HttpPatch("{id:int}/cancelar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CancelarInscripcion(int id)
    {
        var (exito, error) = _service.CancelarInscripcion(id);
        if (!exito)
        {
            return BadRequest(new { mensaje = error });
        }

        return Ok(new { mensaje = $"La inscripción #{id} ha sido cancelada y el cupo fue reintegrado al curso." });
    }
}
