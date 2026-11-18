using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class IngredienteEndpoints
{
    public static IEndpointRouteBuilder MapIngredienteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingredientes");

        // GET /api/ingredientes — catálogo para el multi-select del panel
        // admin y para la personalización de pizzas (Fase 8).
        group.MapGet("/", async (IIngredienteService ingredienteService) =>
        {
            var ingredientes = await ingredienteService.ObtenerIngredientesAsync();
            return Results.Ok(ingredientes);
        });

        return app;
    }
}
