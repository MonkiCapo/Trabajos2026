using Api_Institucion.Dtos;
using Api_Institucion.Models;
using Api_Institucion.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api_Institucion.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocentesController : ControllerBase
{
    private readonly IInstitucionService _service;

    public DocentesController(IInstitucionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta el listado de docentes registrados.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Docente>), StatusCodes.Status200OK)]
    public IActionResult ObtenerTodos()
    {
        return Ok(_service.ObtenerDocentes());
    }

    /// <summary>
    /// Consulta los datos de un docente por su legajo.
    /// </summary>
    [HttpGet("{legajo}")]
    [ProducesResponseType(typeof(Docente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ObtenerPorLegajo(string legajo)
    {
        var docente = _service.ObtenerDocentePorLegajo(legajo);
        if (docente is null)
        {
            return NotFound(new { mensaje = $"No se encontró el docente con legajo '{legajo}'." });
        }
        return Ok(docente);
    }

    /// <summary>
    /// Registra un nuevo docente en el sistema (RF1).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Docente), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RegistrarDocente([FromBody] CrearDocenteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Legajo) || string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
        {
            return BadRequest(new { mensaje = "Legajo, Nombre y Apellido son obligatorios." });
        }

        var (docente, error) = _service.RegistrarDocente(dto);
        if (docente is null)
        {
            return BadRequest(new { mensaje = error });
        }

        return CreatedAtAction(nameof(ObtenerPorLegajo), new { legajo = docente.Legajo }, docente);
    }

    /// <summary>
    /// Consulta los cursos a los que está asignado el docente (RF3 / Caso Docente / consultarCursos() del apunte UML).
    /// </summary>
    [HttpGet("{legajo}/cursos")]
    [ProducesResponseType(typeof(IEnumerable<CursoResponseDto>), StatusCodes.Status200OK)]
    public IActionResult ObtenerCursosAsignados(string legajo)
    {
        var docente = _service.ObtenerDocentePorLegajo(legajo);
        if (docente is null)
        {
            return NotFound(new { mensaje = $"No se encontró el docente con legajo '{legajo}'." });
        }

        var cursos = _service.ObtenerCursosPorDocente(legajo);
        return Ok(cursos);
    }
}
