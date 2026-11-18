using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class TamanioEndpoints
{
    public static IEndpointRouteBuilder MapTamanioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tamanios");

        // GET /api/tamanios (Tamaños con su multiplicador) — publico:
        // el catalogo lo usa para mostrar el precio de cada tamaño.
        group.MapGet("/", async (ITamanioService tamanioService) =>
        {
            var tamanios = await tamanioService.ObtenerTamaniosAsync();
            return Results.Ok(tamanios);
        });

        // PUT /api/tamanios/{id} (Editar multiplicador) — solo Admin.
        // El nombre no se edita: los items historicos guardan el nombre y
        // renombrarlo romperia la referencia de esos pedidos.
        group.MapPut("/{id:int}", async (int id, TamanioUpdateRequest request, ITamanioService tamanioService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TamanioEndpoints");

            try
            {
                var tamanio = await tamanioService.ActualizarMultiplicadorAsync(id, request);
                if (tamanio == null)
                {
                    return Results.NotFound();
                }

                logger.LogInformation("[API] Tamanio {Id} ({Nombre}) actualizado a x{Multiplicador}.",
                    tamanio.Id, tamanio.Nombre, tamanio.Multiplicador);
                return Results.Ok(tamanio);
            }
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error inesperado al actualizar el tamanio {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("Admin");

        return app;
    }
}
