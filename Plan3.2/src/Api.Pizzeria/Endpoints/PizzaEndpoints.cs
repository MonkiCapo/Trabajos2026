using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        return app;
    }
}
