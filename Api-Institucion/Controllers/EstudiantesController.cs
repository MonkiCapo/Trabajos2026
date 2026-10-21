using Api_Institucion.Dtos;
using Api_Institucion.Models;
using Api_Institucion.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api_Institucion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EstudiantesController : ControllerBase
{
    private readonly IInstitucionService _service;

    public EstudiantesController(IInstitucionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta el listado de todos los estudiantes registrados.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Estudiante>), StatusCodes.Status200OK)]
    public IActionResult ObtenerTodos()
    {
        return Ok(_service.ObtenerEstudiantes());
    }

    /// <summary>
    /// Consulta los datos de un estudiante por su legajo.
    /// </summary>
    [HttpGet("{legajo}")]
    [ProducesResponseType(typeof(Estudiante), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ObtenerPorLegajo(string legajo)
    {
        var estudiante = _service.ObtenerEstudiantePorLegajo(legajo);
        if (estudiante is null)
        {
            return NotFound(new { mensaje = $"No se encontró el estudiante con legajo '{legajo}'." });
        }
        return Ok(estudiante);
    }

    /// <summary>
    /// Registra un nuevo estudiante en el sistema (RF1).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Estudiante), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RegistrarEstudiante([FromBody] CrearEstudianteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Legajo) || string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
        {
            return BadRequest(new { mensaje = "Legajo, Nombre y Apellido son obligatorios." });
        }

        var (estudiante, error) = _service.RegistrarEstudiante(dto);
        if (estudiante is null)
        {
            return BadRequest(new { mensaje = error });
        }

        return CreatedAtAction(nameof(ObtenerPorLegajo), new { legajo = estudiante.Legajo }, estudiante);
    }

    /// <summary>
    /// Consulta el historial de inscripciones que realizó el estudiante (Método consultarInscripciones() del apunte UML).
    /// </summary>
    [HttpGet("{legajo}/inscripciones")]
    [ProducesResponseType(typeof(IEnumerable<InscripcionResponseDto>), StatusCodes.Status200OK)]
    public IActionResult ObtenerInscripciones(string legajo)
    {
        var estudiante = _service.ObtenerEstudiantePorLegajo(legajo);
        if (estudiante is null)
        {
            return NotFound(new { mensaje = $"No se encontró el estudiante con legajo '{legajo}'." });
        }

        var inscripciones = _service.ObtenerInscripcionesPorEstudiante(legajo);
        return Ok(inscripciones);
    }
}
