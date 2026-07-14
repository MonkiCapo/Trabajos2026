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
using Api.Pizzeria.Sockets;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Dapper.Pizzeria;
using Api.Pizzeria.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar registro (logging)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configurar opciones JSON para mapear enums como strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Configurar cadena de conexión de MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=5to_Pizzeria;User=5to_agbd;Password=Trigg3rs!;";

// Inicializar base de datos usando script.sql
DbInitializer.Initialize(connectionString);

// Registrar servidor de Sockets como Singleton Hosted Service
builder.Services.AddSingleton<SocketServer>();
builder.Services.AddSingleton<ISocketServer>(sp => sp.GetRequiredService<SocketServer>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketServer>());

// Registrar servicios de negocio
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Registrar validadores de FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Registrar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PizzeriaAPI", Version = "v1" });
});

var app = builder.Build();

// Habilitar Swagger en modo Desarrollo
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PizzeriaAPI v1");
    c.RoutePrefix = "swagger";
});

// Endpoints

// 1. POST /api/clientes (Registrar cliente)
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
        logger.LogInformation("[API] Nuevo cliente registrado: {Nombre} (ID: {Id})", cliente.Nombre, cliente.Id);
        return Results.Created($"/api/clientes/{cliente.Id}", cliente);
    }
    catch (MySqlException ex) when (ex.Number == 1062)
    {
        return Results.BadRequest(new { error = "Ya existe un cliente con ese email." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[API] Error al registrar cliente.");
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

// 2. GET /api/pizzas (Catálogo de Pizzas)
app.MapGet("/api/pizzas", async () =>
{
    using var conn = new MySqlConnection(connectionString);
    var pizzas = await conn.QueryAsync<Pizza>("SELECT id, nombre, tamanio, precio, descripcion FROM PIZZA");
    return Results.Ok(pizzas);
});

// 3. POST /api/pedidos (Crear pedido - CU-03)
app.MapPost("/api/pedidos", async (PedidoRequest request, IPedidoService pedidoService, IValidator<PedidoRequest> validator, ILogger<Program> logger) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    // Resolver email a ID de cliente
    Pedido? order = null;
    using (var conn = new MySqlConnection(connectionString))
    {
        var clienteId = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT id FROM CLIENTE WHERE email = @Email", new { Email = request.ClienteEmail });

        if (clienteId == null)
        {
            return Results.BadRequest(new { error = $"No se encontro un cliente con el email '{request.ClienteEmail}'." });
        }

        order = new Pedido
        {
            ClienteId = clienteId.Value,
            Items = request.Items.Select(i => new ItemPedido
            {
                PizzaNombre = i.PizzaNombre,
                Cantidad = i.Cantidad
            }).ToList()
        };
    }

    try
    {
        var createdOrder = await pedidoService.CrearPedidoAsync(order!);

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
        logger.LogWarning("[API] Pedido {Id} cancelado porque Cocina no está disponible.", ex.PedidoId);
        return Results.Json(new
        {
            error = "Servicio de cocina no disponible en este momento",
            pedidoId = ex.PedidoId,
            codigo = "COCINA_NO_DISPONIBLE"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[API] Error inesperado al crear pedido.");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

// 4. GET /api/pedidos/{id} (Consultar pedido - CU-02)
app.MapGet("/api/pedidos/{id}", async (int id, IPedidoService pedidoService, ILogger<Program> logger) =>
{
    try
    {
        var order = await pedidoService.GetPedidoByIdAsync(id);
        if (order == null)
        {
            return Results.NotFound();
        }

        // Obtener información del cliente
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
        logger.LogError(ex, "[API] Error al obtener pedido {Id}.", id);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.Run();
