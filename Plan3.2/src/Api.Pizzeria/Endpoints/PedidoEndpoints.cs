using System;
using System.Linq;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Excepciones;
using Core.Pizzeria.Servicios.IService;

namespace Api.Pizzeria.Endpoints;

public static class PedidoEndpoints
{
    private const string CLAIM_CLIENTE_ID = "ClienteId";

    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos");

        // 3. POST /api/pedidos (Crear pedido - CU-03) — solo Admin
        group.MapPost("/", async (PedidoRequest request, IPedidoService pedidoService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");

            try
            {
                // El servicio valida el request y resuelve el cliente: el
                // endpoint solo traduce las excepciones de dominio a HTTP.
                var createdOrder = await pedidoService.CrearPedidoAsync(request);

                return Results.Created($"/api/pedidos/{createdOrder.Id}", new
                {
                    pedidoId = createdOrder.Id,
                    estado = createdOrder.Estado,
                    total = createdOrder.Total,
                    fechaCreacion = createdOrder.FechaCreacion
                });
            }
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
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
        }).RequireAuthorization("Admin");

        // 4. GET /api/pedidos/{id} (Consultar pedido - CU-02)
        // Ownership: solo el dueno del pedido (o un Admin) puede verlo;
        // el 403 no revela si el pedido existe o no.
        group.MapGet("/{id:int}", async (int id, IPedidoService pedidoService, IClienteService clienteService, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");
            try
            {
                var order = await pedidoService.GetPedidoByIdAsync(id);
                if (order == null)
                {
                    return Results.NotFound();
                }

                if (!EsDuenoDelPedido(httpContext, order.ClienteId) && !httpContext.User.IsInRole("Admin"))
                {
                    return Results.Forbid();
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
                        pizzaId = i.PizzaId,
                        pizza = i.PizzaNombre,
                        tamanio = i.Tamanio,
                        cantidad = i.Cantidad,
                        precioUnitario = i.PrecioUnitario,
                        quitar = i.Quitar,
                        extras = i.Extras
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
        }).RequireAuthorization();

        // 5. PATCH /api/pedidos/{id}/estado (Avanzar estado del pedido) — solo Admin
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
            catch (ConflictoEstadoPedidoException ex)
            {
                // Otro proceso cambio el estado entre la lectura y el UPDATE condicional.
                logger.LogWarning("[API] Conflicto de concurrencia en el pedido {Id}: {Motivo}", id, ex.Message);
                return Results.Conflict(new { error = "Conflicto de estado", detalles = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // Transicion no permitida por la maquina de estados.
                logger.LogWarning("[API] Transicion rechazada en el pedido {Id}: {Motivo}", id, ex.Message);
                return Results.BadRequest(new { error = "Transicion invalida", detalles = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al transicionar el estado del pedido {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization("Admin");

        // 6. GET /api/pedidos/{id}/historial (Historial de estados - UC-02)
        // Mismo ownership que el GET del pedido.
        group.MapGet("/{id:int}/historial", async (int id, IPedidoService pedidoService, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");
            try
            {
                var pedido = await pedidoService.GetPedidoByIdAsync(id);
                if (pedido == null)
                {
                    return Results.NotFound();
                }

                if (!EsDuenoDelPedido(httpContext, pedido.ClienteId) && !httpContext.User.IsInRole("Admin"))
                {
                    return Results.Forbid();
                }

                var historial = await pedidoService.ObtenerHistorialAsync(id);

                return Results.Ok(historial.Select(h => new
                {
                    estado = h.Estado,
                    fechaCambio = h.FechaCambio,
                    observacion = h.Observacion
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error al obtener el historial del pedido {Id}.", id);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization();

        // 3. POST /api/pedidos/checkout (Cerrar pedido del cliente logueado)
        // El cliente sale del token (claim ClienteId), nunca del body.
        group.MapPost("/checkout", async (CheckoutRequest request, IPedidoService pedidoService, HttpContext httpContext, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PedidoEndpoints");

            var clienteIdClaim = httpContext.User.FindFirstValue(CLAIM_CLIENTE_ID);
            if (!int.TryParse(clienteIdClaim, out var clienteId))
            {
                logger.LogWarning("[API] Checkout rechazado: token sin claim ClienteId.");
                return Results.Unauthorized();
            }

            try
            {
                var createdOrder = await pedidoService.CrearPedidoConDatosAsync(clienteId, request);

                logger.LogInformation("[API] Checkout exitoso: pedido {Id} por cliente {ClienteId}", createdOrder.Id, clienteId);

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
            catch (ValidationException ex)
            {
                return ValidacionHelper.RespuestaDe(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[API] Error inesperado al hacer el checkout.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }).RequireAuthorization();

        return app;
    }

    private static bool EsDuenoDelPedido(HttpContext httpContext, int clienteId)
    {
        var claim = httpContext.User.FindFirstValue(CLAIM_CLIENTE_ID);
        return int.TryParse(claim, out var clienteIdDelToken) && clienteIdDelToken == clienteId;
    }
}
