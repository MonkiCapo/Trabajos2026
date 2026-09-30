using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // 1. POST /api/auth/registro (Crear cliente + usuario)
        group.MapPost("/registro", async (RegistroRequest request, IAuthService authService, IValidator<RegistroRequest> validator, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            try
            {
                var usuario = await authService.RegistrarAsync(request);
                logger.LogInformation("[API] Nuevo usuario registrado: {Email} (Rol: {Rol})", usuario.Email, usuario.Rol);
                return Results.Created($"/api/auth/usuarios/{usuario.ClienteId}", usuario);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al registrar el usuario.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        // 2. POST /api/auth/login (Iniciar sesion)
        group.MapPost("/login", async (LoginRequest request, IAuthService authService, IValidator<LoginRequest> validator, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            try
            {
                var usuario = await authService.LoginAsync(request);

                // 401 tanto si el email no existe como si la contraseña no coincide,
                // para no revelar que emails estan registrados.
                if (usuario == null)
                {
                    logger.LogWarning("[API] Credenciales invalidas para {Email}", request.Email);
                    return Results.Json(
                        new { error = "Email o contraseña incorrectos." },
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                return Results.Ok(usuario);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al iniciar sesion.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        return app;
    }
}
