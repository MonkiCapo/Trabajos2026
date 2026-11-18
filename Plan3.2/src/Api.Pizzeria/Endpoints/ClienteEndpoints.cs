using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class ClienteEndpoints
{
    private const string CLAIM_CLIENTE_ID = "ClienteId";

    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes");

        // POST /api/clientes fue eliminado (decision 2026-11-18): creaba
        // clientes "huerfanos" sin usuario. UC-01 queda cubierto por
        // POST /api/auth/registro, que crea CLIENTE + USUARIO en una transaccion.

        // GET /api/clientes/{id} (Obtener cliente por ID)
        // Ownership: los datos (email, telefono, direccion) son privados, asi
        // que solo el propio cliente (o un Admin) puede verlos; el 403 no
        // revela si el id existe o no.
        group.MapGet("/{id:int}", async (int id, IClienteService clienteService, HttpContext httpContext) =>
        {
            if (!EsElPropioCliente(httpContext, id) && !httpContext.User.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var cliente = await clienteService.ObtenerClientePorIdAsync(id);
            return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
        }).RequireAuthorization();

        // GET /api/clientes/email/{email} (Obtener cliente por email) — solo Admin
        group.MapGet("/email/{email}", async (string email, IClienteService clienteService) =>
        {
            var cliente = await clienteService.ObtenerClientePorEmailAsync(email);
            return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
        }).RequireAuthorization("Admin");

        return app;
    }

    private static bool EsElPropioCliente(HttpContext httpContext, int clienteId)
    {
        var claim = httpContext.User.FindFirstValue(CLAIM_CLIENTE_ID);
        return int.TryParse(claim, out var clienteIdDelToken) && clienteIdDelToken == clienteId;
    }
}
