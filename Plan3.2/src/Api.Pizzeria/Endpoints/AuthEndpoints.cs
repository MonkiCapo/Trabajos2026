using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // 1. POST /api/auth/registro (Crear cliente + usuario) — cubre UC-01
        group.MapPost("/registro", async (RegistroRequest request, IAuthService authService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");

            try
            {
                var auth = await authService.RegistrarAsync(request);
                logger.LogInformation("[API] Nuevo usuario registrado: {Email} (Rol: {Rol})", Pii.AnonimizarEmail(auth.Usuario.Email), auth.Usuario.Rol);
                return Results.Created($"/api/auth/usuarios/{auth.Usuario.ClienteId}", auth);
            }
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al registrar el usuario.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        // 2. POST /api/auth/login (Iniciar sesion) — devuelve { token, usuario }
        group.MapPost("/login", async (LoginRequest request, IAuthService authService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");

            try
            {
                var auth = await authService.LoginAsync(request);

                // 401 tanto si el email no existe como si la contraseña no coincide,
                // para no revelar que emails estan registrados.
                if (auth == null)
                {
                    logger.LogWarning("[API] Credenciales invalidas para {Email}", Pii.AnonimizarEmail(request.Email));
                    return Results.Json(
                        new { error = "Email o contraseña incorrectos." },
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                return Results.Ok(auth);
            }
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al iniciar sesion.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        // 3. GET /api/auth/me/{usuarioId} (Datos del usuario autenticado)
        // Ownership: solo el propio usuario (o un Admin) puede ver su perfil;
        // el 403 no revela si el id existe o no.
        group.MapGet("/me/{usuarioId:int}", async (int usuarioId, IAuthService authService, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");

            if (!EsElPropioUsuario(httpContext, usuarioId) && !httpContext.User.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            try
            {
                var usuario = await authService.ObtenerPerfilAsync(usuarioId);
                if (usuario == null)
                {
                    return Results.NotFound(new { error = $"No se encontro el usuario con id {usuarioId}." });
                }

                return Results.Ok(usuario);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al obtener el perfil del usuario {UsuarioId}.", usuarioId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization();

        return app;
    }

    private static bool EsElPropioUsuario(HttpContext httpContext, int usuarioId)
    {
        var claim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var usuarioIdDelToken) && usuarioIdDelToken == usuarioId;
    }
}
