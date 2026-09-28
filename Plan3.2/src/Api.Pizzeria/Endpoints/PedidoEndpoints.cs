using System;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos");

        // 3. POST /api/pedidos (Crear pedido - CU-03)
        group.MapPost("/", async (PedidoRequest request, IPedidoService pedidoService, IClienteService clienteService, IValidator<PedidoRequest> validator, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            // Resolver email a ID de cliente
            var cliente = await clienteService.ObtenerClientePorEmailAsync(request.ClienteEmail);
            if (cliente == null)
            {
                return Results.BadRequest(new { error = $"No se encontro un cliente con el email '{request.ClienteEmail}'." });
            }

            var order = new Pedido
            {
                ClienteId = cliente.Id,
                Items = request.Items.Select(i => new ItemPedido
                {
                    PizzaNombre = i.PizzaNombre,
                    Cantidad = i.Cantidad
                }).ToList()
            };

            try
            {
                var createdOrder = await pedidoService.CrearPedidoAsync(order);

                return Results.Created($"/api/pedidos/{createdOrder.Id}", new
                {
                    pedidoId = createdOrder.Id,
                    estado = createdOrder.Estado,
                    total = createdOrder.Total,
                    fechaCreacion = createdOrder.FechaCreacion
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = "Datos invalidos", detalles = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error inesperado al crear pedido.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        // 4. GET /api/pedidos/{id} (Consultar pedido - CU-02)
        group.MapGet("/{id:int}", async (int id, IPedidoService pedidoService, IClienteService clienteService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");
            try
            {
                var order = await pedidoService.GetPedidoByIdAsync(id);
                if (order == null)
                {
                    return Results.NotFound();
                }

                // Obtener información del cliente
                var cliente = await clienteService.ObtenerClientePorIdAsync(order.ClienteId);

                return Results.Ok(new
                {
                    pedidoId = order.Id,
                    estado = order.Estado,
                    cliente = cliente != null ? new { id = cliente.Id, nombre = cliente.Nombre, email = cliente.Email } : null,
                    items = order.Items.Select(i => new
                    {
                        pizza = i.PizzaNombre,
                        cantidad = i.Cantidad,
                        precioUnitario = i.PrecioUnitario
                    }),
                    total = order.Total,
                    fechaCreacion = order.FechaCreacion,
                    ultimaActualizacion = order.FechaActualizacion
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al obtener pedido {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        // 5. PATCH /api/pedidos/{id}/estado (Avanzar estado del pedido)
        group.MapPatch("/{id:int}/estado", async (int id, ActualizarEstadoRequest request, IPedidoService pedidoService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");
            try
            {
                var pedido = await pedidoService.GetPedidoByIdAsync(id);
                if (pedido == null)
                {
                    return Results.NotFound();
                }

                await pedidoService.ActualizarEstadoAsync(id, request.Estado, request.Observacion);

                logger.LogInformation("[API] Pedido {Id} transicionado a {Estado}.", id, request.Estado);
                return Results.Ok(new { pedidoId = id, estado = request.Estado });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al transicionar el estado del pedido {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        });

        return app;
    }
}
