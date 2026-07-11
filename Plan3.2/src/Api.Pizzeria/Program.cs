using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;
using Api.Pizzeria.Data;
using Api.Pizzeria.Models;
using Api.Pizzeria.Services;
using Api.Pizzeria.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add JSON configuration to map enum as strings (optional, but very clean for clients)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Configure SQLite Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=pizzeria.db";

// Initialize Database using script.sql
DbInitializer.Initialize(connectionString);

// Register Sockets Server as Singleton Hosted Service
builder.Services.AddSingleton<SocketServer>();
builder.Services.AddSingleton<ISocketServer>(sp => sp.GetRequiredService<SocketServer>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketServer>());

// Register Business Services
builder.Services.AddScoped<IPedidoService, PedidoService>();

var app = builder.Build();

// Endpoints

// 1. POST /api/clientes (Registrar Cliente)
app.MapPost("/api/clientes", async (Cliente cliente, IConfiguration config, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(cliente.Nombre) || 
        string.IsNullOrWhiteSpace(cliente.Telefono) || 
        string.IsNullOrWhiteSpace(cliente.Direccion))
    {
        return Results.BadRequest(new { error = "Nombre, Teléfono y Dirección son obligatorios." });
    }

    try
    {
        using var conn = new SqliteConnection(connectionString);
        const string sql = @"
            INSERT INTO CLIENTE (nombre, telefono, direccion) 
            VALUES (@Nombre, @Telefono, @Direccion);
            SELECT last_insert_rowid();";
        
        var id = await conn.ExecuteScalarAsync<int>(sql, cliente);
        cliente.Id = id;
        logger.LogInformation("[API] Registered new client: {Nombre} (ID: {Id})", cliente.Nombre, cliente.Id);
        return Results.Created($"/api/clientes/{cliente.Id}", cliente);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[API] Error registering client.");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

// GET /api/clientes/{id} (Troubleshooting / Auxiliary)
app.MapGet("/api/clientes/{id}", async (int id) =>
{
    using var conn = new SqliteConnection(connectionString);
    var cliente = await conn.QuerySingleOrDefaultAsync<Cliente>(
        "SELECT id, nombre, telefono, direccion FROM CLIENTE WHERE id = @Id", new { Id = id });
    
    return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
});

// 2. GET /api/pizzas (Catalogo de Pizzas)
app.MapGet("/api/pizzas", async () =>
{
    using var conn = new SqliteConnection(connectionString);
    var pizzas = await conn.QueryAsync<Pizza>("SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA");
    return Results.Ok(pizzas);
});

// 3. POST /api/pedidos (Crear Pedido - CU-03)
app.MapPost("/api/pedidos", async (PedidoRequest request, IPedidoService pedidoService, ILogger<Program> logger) =>
{
    if (request.ClienteId <= 0 || request.Items == null || !request.Items.Any())
    {
        return Results.BadRequest(new
        {
            error = "Datos inválidos",
            detalles = new
            {
                clienteId = request.ClienteId <= 0 ? new[] { "El campo clienteId es obligatorio" } : null,
                items = (request.Items == null || !request.Items.Any()) ? new[] { "Debe contener al menos un item" } : null
            }
        });
    }

    // Build the Pedido entity
    var order = new Pedido
    {
        ClienteId = request.ClienteId,
        Items = request.Items.Select(i => new ItemPedido
        {
            PizzaId = i.PizzaId,
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
        return Results.BadRequest(new { error = "Datos inválidos", detalles = ex.Message });
    }
    catch (CocinaNoDisponibleException ex)
    {
        logger.LogWarning("[API] Order {Id} cancelled because Cocina is not available.", ex.PedidoId);
        return Results.Json(new
        {
            error = "Servicio de cocina no disponible en este momento",
            pedidoId = ex.PedidoId,
            codigo = "COCINA_NO_DISPONIBLE"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[API] Unexpected error creating order.");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

// 4. GET /api/pedidos/{id} (Consultar Pedido - CU-02)
app.MapGet("/api/pedidos/{id}", async (int id, IPedidoService pedidoService, ILogger<Program> logger) =>
{
    try
    {
        var order = await pedidoService.GetPedidoByIdAsync(id);
        if (order == null)
        {
            return Results.NotFound();
        }

        // Get Client info
        using var conn = new SqliteConnection(connectionString);
        var cliente = await conn.QuerySingleOrDefaultAsync<Cliente>(
            "SELECT id, nombre, telefono, direccion FROM CLIENTE WHERE id = @Id", new { Id = order.ClienteId });

        return Results.Ok(new
        {
            pedidoId = order.Id,
            estado = order.Estado,
            cliente = cliente != null ? new { id = cliente.Id, nombre = cliente.Nombre } : null,
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
        logger.LogError(ex, "[API] Error getting order {Id}.", id);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.Run("http://localhost:5000");

// Request support DTOs
public class PedidoRequest
{
    public int ClienteId { get; set; }
    public List<ItemRequest> Items { get; set; } = new();
}

public class ItemRequest
{
    public int PizzaId { get; set; }
    public int Cantidad { get; set; }
}
