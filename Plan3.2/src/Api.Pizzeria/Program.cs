using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dapper;
using Api.Pizzeria.Data;
using Api.Pizzeria.DTOs;
using Api.Pizzeria.Models;
using Api.Pizzeria.Services;
using Api.Pizzeria.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add JSON configuration to map enum as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Configure MySQL Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=5to_Pizzeria;User=root;Password=;";

// Initialize Database using script.sql
DbInitializer.Initialize(connectionString);

// Register Sockets Server as Singleton Hosted Service
builder.Services.AddSingleton<SocketServer>();
builder.Services.AddSingleton<ISocketServer>(sp => sp.GetRequiredService<SocketServer>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketServer>());

// Register Business Services
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PizzeriaAPI", Version = "v1" });
});

var app = builder.Build();

// Enable Swagger in Development
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PizzeriaAPI v1");
    c.RoutePrefix = "swagger";
});

// Endpoints

// 1. POST /api/clientes (Registrar Cliente)
app.MapPost("/api/clientes", async (ClienteRequest clienteRequest, IValidator<ClienteRequest> validator, ILogger<Program> logger) =>
{
    var validation = await validator.ValidateAsync(clienteRequest);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    try
    {
        using var conn = new MySqlConnection(connectionString);
        const string sql = @"
            INSERT INTO CLIENTE (nombre, email, telefono, direccion) 
            VALUES (@Nombre, @Email, @Telefono, @Direccion);
            SELECT LAST_INSERT_ID();";

        var cliente = new Cliente
        {
            Nombre = clienteRequest.Nombre,
            Email = clienteRequest.Email,
            Telefono = clienteRequest.Telefono,
            Direccion = clienteRequest.Direccion
        };

        var id = await conn.ExecuteScalarAsync<int>(sql, cliente);
        cliente.Id = id;
        logger.LogInformation("[API] Registered new client: {Nombre} (ID: {Id})", cliente.Nombre, cliente.Id);
        return Results.Created($"/api/clientes/{cliente.Id}", cliente);
    }
    catch (MySqlException ex) when (ex.Number == 1062)
    {
        return Results.BadRequest(new { error = "Ya existe un cliente con ese email." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[API] Error registering client.");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

// GET /api/clientes/{id} (Obtener cliente por ID)
app.MapGet("/api/clientes/{id}", async (int id) =>
{
    using var conn = new MySqlConnection(connectionString);
    var cliente = await conn.QuerySingleOrDefaultAsync<Cliente>(
        "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE id = @Id", new { Id = id });

    return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
});

// GET /api/clientes/email/{email} (Obtener cliente por email)
app.MapGet("/api/clientes/email/{email}", async (string email) =>
{
    using var conn = new MySqlConnection(connectionString);
    var cliente = await conn.QuerySingleOrDefaultAsync<Cliente>(
        "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE email = @Email", new { Email = email });

    return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
});

// 2. GET /api/pizzas (Catalogo de Pizzas)
app.MapGet("/api/pizzas", async () =>
{
    using var conn = new MySqlConnection(connectionString);
    var pizzas = await conn.QueryAsync<Pizza>("SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA");
    return Results.Ok(pizzas);
});

// 3. POST /api/pedidos (Crear Pedido - CU-03)
app.MapPost("/api/pedidos", async (PedidoRequest request, IPedidoService pedidoService, IValidator<PedidoRequest> validator, ILogger<Program> logger) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    // Build the Pedido entity
    var order = new Pedido
    {
        ClienteId = request.ClienteId,
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
        using var conn = new MySqlConnection(connectionString);
        var cliente = await conn.QuerySingleOrDefaultAsync<Cliente>(
            "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE id = @Id", new { Id = order.ClienteId });

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
        logger.LogError(ex, "[API] Error getting order {Id}.", id);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.Run();
