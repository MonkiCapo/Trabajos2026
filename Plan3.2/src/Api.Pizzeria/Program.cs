using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Api.Pizzeria.Data;
using Api.Pizzeria.Sockets;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;
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

// Registrar cadena de conexión de MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=5to_Pizzeria;User=5to_agbd;Password=Trigg3rs!;";

// Registrar Ado (conexión a base de datos)
builder.Services.AddSingleton<IAdo>(new Ado(connectionString));

// Registrar repositorios
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<IPizzaRepositorio, PizzaRepositorio>();

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
app.MapPost("/api/clientes", async (ClienteRequest clienteRequest, IValidator<ClienteRequest> validator, IClienteRepositorio clienteRepo, ILogger<Program> logger) =>
{
    var validation = await validator.ValidateAsync(clienteRequest);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    try
    {
        var cliente = new Cliente
        {
            Nombre = clienteRequest.Nombre,
            Email = clienteRequest.Email,
            Telefono = clienteRequest.Telefono,
            Direccion = clienteRequest.Direccion
        };

        var id = await clienteRepo.AgregarClienteAsync(cliente);
        cliente.Id = id;
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
app.MapGet("/api/clientes/{id}", async (int id, IClienteRepositorio clienteRepo) =>
{
    var cliente = await clienteRepo.ObtenerClientePorIdAsync(id);
    return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
});

// GET /api/clientes/email/{email} (Obtener cliente por email)
app.MapGet("/api/clientes/email/{email}", async (string email, IClienteRepositorio clienteRepo) =>
{
    var cliente = await clienteRepo.ObtenerClientePorEmailAsync(email);
    return cliente is not null ? Results.Ok(cliente) : Results.NotFound();
});

// 2. GET /api/pizzas (Catálogo de Pizzas)
app.MapGet("/api/pizzas", async (IPizzaRepositorio pizzaRepo) =>
{
    var pizzas = await pizzaRepo.ObtenerPizzasAsync();
    return Results.Ok(pizzas);
});

// 3. POST /api/pedidos (Crear pedido - CU-03)
app.MapPost("/api/pedidos", async (PedidoRequest request, IPedidoService pedidoService, IClienteRepositorio clienteRepo, IValidator<PedidoRequest> validator, ILogger<Program> logger) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    // Resolver email a ID de cliente
    var cliente = await clienteRepo.ObtenerClientePorEmailAsync(request.ClienteEmail);
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
app.MapGet("/api/pedidos/{id}", async (int id, IPedidoService pedidoService, IClienteRepositorio clienteRepo, ILogger<Program> logger) =>
{
    try
    {
        var order = await pedidoService.GetPedidoByIdAsync(id);
        if (order == null)
        {
            return Results.NotFound();
        }

        // Obtener información del cliente
        var cliente = await clienteRepo.ObtenerClientePorIdAsync(order.ClienteId);

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
