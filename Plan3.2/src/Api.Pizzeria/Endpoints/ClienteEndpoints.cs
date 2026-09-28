using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes");

        // 1. POST /api/clientes (Registrar cliente)
        group.MapPost("/", async (ClienteRequest clienteRequest, IValidator<ClienteRequest> validator, IClienteService clienteService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ClienteEndpoints");
            var validation = await validator.ValidateAsync(clienteRequest);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            try
            {
                var cliente = await clienteService.AgregarClienteAsync(clienteRequest);
                logger.LogInformation("[API] Nuevo cliente registrado: {Nombre} (ID: {Id})", cliente.Nombre, cliente.Id);
                return Results.Created($"/api/clientes/{cliente.Id}", cliente);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al registrar cliente.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        // GET /api/clientes/{id} (Obtener cliente por ID)
        group.MapGet("/{id:int}", async (int id, IClienteService clienteService) =>
        {
            var cliente = await clienteService.ObtenerClientePorIdAsync(id);
            return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
        });

        // GET /api/clientes/email/{email} (Obtener cliente por email)
        group.MapGet("/email/{email}", async (string email, IClienteService clienteService) =>
        {
            var cliente = await clienteService.ObtenerClientePorEmailAsync(email);
            return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
        });

        return app;
    }
}
