using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api.Pizzeria.Data;
using Api.Pizzeria.Middleware;
using Api.Pizzeria.Services;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;
using Dapper.Pizzeria;

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

// Manejo centralizado de errores (404/409/400/500 según la excepción)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Habilitar Swagger en modo Desarrollo
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PizzeriaAPI v1");
    c.RoutePrefix = "swagger";
});

// Mapeos entidad -> DTO de respuesta
static ClienteResponse ToClienteResponse(Cliente c) => new()
{
    Id = c.Id,
    Nombre = c.Nombre,
    Email = c.Email,
    Telefono = c.Telefono,
    Direccion = c.Direccion
};

static PizzaResponse ToPizzaResponse(Pizza p) => new()
{
    Id = p.Id,
    Nombre = p.Nombre,
    Tamanio = p.Tamanio,
    Precio = p.Precio,
    Descripcion = p.Descripcion,
    Stock = p.Stock,
    Disponible = p.Disponible
};

static PedidoDetalleResponse ToPedidoDetalleResponse(Pedido p, Cliente? cliente) => new()
{
    PedidoId = p.Id,
    Estado = p.Estado,
    Cliente = cliente is null ? null : new ClienteResumenResponse
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        Email = cliente.Email
    },
    Items = p.Items.Select(i => new ItemPedidoResponse
    {
        Pizza = i.PizzaNombre,
        Cantidad = i.Cantidad,
        PrecioUnitario = i.PrecioUnitario
    }).ToList(),
    Total = p.Total,
    FechaCreacion = p.FechaCreacion,
    FechaActualizacion = p.FechaActualizacion
};

// Endpoints

// 1. POST /api/clientes (Registrar cliente)
app.MapPost("/api/clientes", async (ClienteRequest clienteRequest, IValidator<ClienteRequest> validator, IClienteRepositorio clienteRepo, ILogger<Program> logger, CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(clienteRequest, ct);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var cliente = new Cliente
    {
        Nombre = clienteRequest.Nombre,
        Email = clienteRequest.Email,
        Telefono = clienteRequest.Telefono,
        Direccion = clienteRequest.Direccion
    };

    cliente.Id = await clienteRepo.AgregarClienteAsync(cliente, ct);
    logger.LogInformation("[API] Nuevo cliente registrado: {Nombre} (ID: {Id})", cliente.Nombre, cliente.Id);
    return Results.Created($"/api/clientes/{cliente.Id}", ToClienteResponse(cliente));
});

// GET /api/clientes/{id} (Obtener cliente por ID)
app.MapGet("/api/clientes/{id}", async (int id, IClienteRepositorio clienteRepo, CancellationToken ct) =>
{
    var cliente = await clienteRepo.ObtenerClientePorIdAsync(id, ct);
    return cliente is not null ? Results.Ok(ToClienteResponse(cliente)) : Results.NotFound();
});

// GET /api/clientes/email/{email} (Obtener cliente por email)
app.MapGet("/api/clientes/email/{email}", async (string email, IClienteRepositorio clienteRepo, CancellationToken ct) =>
{
    var cliente = await clienteRepo.ObtenerClientePorEmailAsync(email, ct);
    return cliente is not null ? Results.Ok(ToClienteResponse(cliente)) : Results.NotFound();
});

// 2. GET /api/pizzas (Catálogo de Pizzas; ?todas=true para ver también las ocultas - admin)
app.MapGet("/api/pizzas", async (bool? todas, IPizzaRepositorio pizzaRepo, CancellationToken ct) =>
{
    var pizzas = await pizzaRepo.ObtenerPizzasAsync(soloDisponibles: todas != true, ct);
    return Results.Ok(pizzas.Select(ToPizzaResponse));
});

// 2b. POST /api/pizzas (Agregar pizza - Administrador)
app.MapPost("/api/pizzas", async (PizzaRequest request, IValidator<PizzaRequest> validator, IPizzaRepositorio pizzaRepo, ILogger<Program> logger, CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    if (await pizzaRepo.ExistePizzaConNombreAsync(request.Nombre, ct: ct))
    {
        return Results.Conflict(new { error = $"Ya existe una pizza con el nombre '{request.Nombre}'." });
    }

    var pizza = new Pizza
    {
        Nombre = request.Nombre,
        Tamanio = request.Tamanio,
        Precio = request.Precio,
        Descripcion = request.Descripcion,
        Stock = request.Stock,
        Disponible = request.Disponible
    };

    pizza.Id = await pizzaRepo.AgregarPizzaAsync(pizza, ct);
    logger.LogInformation("[API] Nueva pizza agregada: {Nombre} (ID: {Id})", pizza.Nombre, pizza.Id);
    return Results.Created($"/api/pizzas/{pizza.Id}", ToPizzaResponse(pizza));
});

// 2c. PUT /api/pizzas/{id} (Editar pizza - Administrador)
app.MapPut("/api/pizzas/{id}", async (int id, PizzaRequest request, IValidator<PizzaRequest> validator, IPizzaRepositorio pizzaRepo, ILogger<Program> logger, CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var pizza = await pizzaRepo.ObtenerPizzaPorIdAsync(id, ct);
    if (pizza == null)
    {
        return Results.NotFound(new { error = $"No se encontró la pizza con ID {id}." });
    }

    if (await pizzaRepo.ExistePizzaConNombreAsync(request.Nombre, id, ct))
    {
        return Results.Conflict(new { error = $"Ya existe otra pizza con el nombre '{request.Nombre}'." });
    }

    pizza.Nombre = request.Nombre;
    pizza.Tamanio = request.Tamanio;
    pizza.Precio = request.Precio;
    pizza.Descripcion = request.Descripcion;
    pizza.Stock = request.Stock;
    pizza.Disponible = request.Disponible;

    await pizzaRepo.ActualizarPizzaAsync(pizza, ct);
    logger.LogInformation("[API] Pizza {Id} actualizada.", id);
    return Results.Ok(ToPizzaResponse(pizza));
});

// 2d. DELETE /api/pizzas/{id} (Eliminar pizza - Administrador)
app.MapDelete("/api/pizzas/{id}", async (int id, IPizzaRepositorio pizzaRepo, ILogger<Program> logger, CancellationToken ct) =>
{
    var pizza = await pizzaRepo.ObtenerPizzaPorIdAsync(id, ct);
    if (pizza == null)
    {
        return Results.NotFound(new { error = $"No se encontró la pizza con ID {id}." });
    }

    if (await pizzaRepo.TieneItemsAsociadosAsync(id, ct))
    {
        return Results.Conflict(new
        {
            error = $"La pizza '{pizza.Nombre}' tiene pedidos asociados y no puede eliminarse. Sugerencia: ocultarla del menú con PUT /api/pizzas/{id} estableciendo disponible = false."
        });
    }

    await pizzaRepo.EliminarPizzaAsync(id, ct);
    logger.LogInformation("[API] Pizza {Id} eliminada.", id);
    return Results.Ok(new { mensaje = $"Pizza '{pizza.Nombre}' eliminada correctamente." });
});

// 3. POST /api/pedidos (Crear pedido - CU-03)
app.MapPost("/api/pedidos", async (PedidoRequest request, IPedidoService pedidoService, IClienteRepositorio clienteRepo, IValidator<PedidoRequest> validator, CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    // Resolver email a ID de cliente
    var cliente = await clienteRepo.ObtenerClientePorEmailAsync(request.ClienteEmail, ct);
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

    var createdOrder = await pedidoService.CrearPedidoAsync(order, ct);

    return Results.Created($"/api/pedidos/{createdOrder.Id}", new
    {
        pedidoId = createdOrder.Id,
        estado = createdOrder.Estado,
        total = createdOrder.Total,
        fechaCreacion = createdOrder.FechaCreacion
    });
});

// 4. GET /api/pedidos/{id} (Consultar pedido - CU-02)
app.MapGet("/api/pedidos/{id}", async (int id, IPedidoService pedidoService, IClienteRepositorio clienteRepo, CancellationToken ct) =>
{
    var order = await pedidoService.GetPedidoByIdAsync(id, ct);
    if (order == null)
    {
        return Results.NotFound();
    }

    var cliente = await clienteRepo.ObtenerClientePorIdAsync(order.ClienteId, ct);
    return Results.Ok(ToPedidoDetalleResponse(order, cliente));
});

// 4b. PUT /api/pedidos/{id} (Modificar pedido - Cliente, solo en EsperaConfirmacion)
app.MapPut("/api/pedidos/{id}", async (int id, ModificarPedidoRequest request, IPedidoService pedidoService, IClienteRepositorio clienteRepo, IValidator<ModificarPedidoRequest> validator, CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var pedido = await pedidoService.ModificarPedidoAsync(id, request.Items, ct);
    var cliente = await clienteRepo.ObtenerClientePorIdAsync(pedido.ClienteId, ct);
    return Results.Ok(ToPedidoDetalleResponse(pedido, cliente));
});

// 5. GET /api/pedidos (Listar pedidos, filtro opcional ?estado=)
app.MapGet("/api/pedidos", async (string? estado, IPedidoService pedidoService, CancellationToken ct) =>
{
    EstadoPedido? filtro = null;
    if (!string.IsNullOrWhiteSpace(estado))
    {
        if (!Enum.TryParse<EstadoPedido>(estado, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            return Results.BadRequest(new { error = $"Estado inválido: '{estado}'. Valores válidos: EsperaConfirmacion, EnPreparacion, EnViaje, Entregado." });
        }
        filtro = parsed;
    }

    var pedidos = await pedidoService.GetPedidosAsync(filtro, ct);
    return Results.Ok(pedidos.Select(p => new PedidoResumenResponse
    {
        PedidoId = p.Id,
        ClienteId = p.ClienteId,
        Estado = p.Estado,
        Total = p.Total,
        FechaCreacion = p.FechaCreacion,
        FechaActualizacion = p.FechaActualizacion
    }));
});

// 6. PUT /api/pedidos/{id}/estado (Cambiar estado - máquina de estados)
app.MapPut("/api/pedidos/{id}/estado", async (int id, CambiarEstadoRequest request, IPedidoService pedidoService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Estado) ||
        !Enum.TryParse<EstadoPedido>(request.Estado, ignoreCase: true, out var nuevoEstado) ||
        !Enum.IsDefined(nuevoEstado))
    {
        return Results.BadRequest(new { error = $"Estado inválido: '{request.Estado}'. Valores válidos: EsperaConfirmacion, EnPreparacion, EnViaje, Entregado." });
    }

    await pedidoService.CambiarEstadoAsync(id, nuevoEstado, ct);
    return Results.Ok(new { pedidoId = id, estado = nuevoEstado });
});

app.Run();