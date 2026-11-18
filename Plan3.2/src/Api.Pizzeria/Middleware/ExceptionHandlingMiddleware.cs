using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Core.Pizzeria.Servicios.Excepciones;

namespace Api.Pizzeria.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (PedidoNoEncontradoException ex)
        {
            _logger.LogWarning("[API] {Path}: {Mensaje}", context.Request.Path, ex.Message);
            await EscribirJsonAsync(context, StatusCodes.Status404NotFound, new { error = ex.Message });
        }
        catch (TransicionInvalidaException ex)
        {
            _logger.LogWarning("[API] Transicion invalida en {Path}: {Actual} -> {Solicitado}", context.Request.Path, ex.EstadoActual, ex.EstadoSolicitado);
            await EscribirJsonAsync(context, StatusCodes.Status409Conflict, new
            {
                error = ex.Message,
                estadoActual = ex.EstadoActual,
                estadoSolicitado = ex.EstadoSolicitado
            });
        }
        catch (PedidoNoModificableException ex)
        {
            _logger.LogWarning("[API] Modificacion rechazada en {Path}: {Mensaje}", context.Request.Path, ex.Message);
            await EscribirJsonAsync(context, StatusCodes.Status409Conflict, new
            {
                error = ex.Message,
                estadoActual = ex.EstadoActual
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            _logger.LogWarning("[API] Registro duplicado en {Path}.", context.Request.Path);
            await EscribirJsonAsync(context, StatusCodes.Status409Conflict, new
            {
                error = "Ya existe un registro con esos datos (por ejemplo, un cliente con ese email)."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("[API] Datos invalidos en {Path}: {Mensaje}", context.Request.Path, ex.Message);
            await EscribirJsonAsync(context, StatusCodes.Status400BadRequest, new { error = "Datos invalidos", detalles = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Error inesperado en {Path}.", context.Request.Path);
            await EscribirJsonAsync(context, StatusCodes.Status500InternalServerError, new { error = "Error interno del servidor." });
        }
    }

    private static async Task EscribirJsonAsync(HttpContext context, int statusCode, object body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}