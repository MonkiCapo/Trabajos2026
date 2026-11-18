using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios.Excepciones;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class PizzaEndpoints
{
    public static IEndpointRouteBuilder MapPizzaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pizzas");

        // 2. GET /api/pizzas (Catálogo de Pizzas)
        group.MapGet("/", async (IPizzaService pizzaService) =>
        {
            var pizzas = await pizzaService.ObtenerPizzasAsync();
            return Results.Ok(pizzas);
        });

        // POST /api/pizzas (Crear pizza) — solo Admin
        group.MapPost("/", async (PizzaRequest request, IPizzaService pizzaService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PizzaEndpoints");

            try
            {
                var pizza = await pizzaService.CrearPizzaAsync(request);
                logger.LogInformation("[API] Pizza {Id} creada: {Nombre}.", pizza.Id, pizza.Nombre);
                return Results.Created($"/api/pizzas/{pizza.Id}", pizza);
            }
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error inesperado al crear la pizza.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("Admin");

        // PUT /api/pizzas/{id} (Editar pizza + reemplazar ingredientes) — solo Admin
        group.MapPut("/{id:int}", async (int id, PizzaRequest request, IPizzaService pizzaService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PizzaEndpoints");

            try
            {
                var pizza = await pizzaService.ActualizarPizzaAsync(id, request);
                if (pizza == null)
                {
                    return Results.NotFound();
                }

                logger.LogInformation("[API] Pizza {Id} actualizada: {Nombre}.", id, pizza.Nombre);
                return Results.Ok(pizza);
            }
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error inesperado al actualizar la pizza {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("Admin");

        // DELETE /api/pizzas/{id} (Eliminar pizza) — solo Admin
        // Si ITEM_PEDIDO la referencia, la FK RESTRICT lo rechaza: 409 con mensaje claro.
        group.MapDelete("/{id:int}", async (int id, IPizzaService pizzaService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PizzaEndpoints");

            try
            {
                var eliminada = await pizzaService.EliminarPizzaAsync(id);
                if (!eliminada)
                {
                    return Results.NotFound();
                }

                logger.LogInformation("[API] Pizza {Id} eliminada.", id);
                return Results.NoContent();
            }
            catch (PizzaEnUsoException ex)
            {
                logger.LogWarning("[API] No se puede eliminar la pizza {Id}: {Motivo}", id, ex.Message);
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error inesperado al eliminar la pizza {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("Admin");

        return app;
    }
}
