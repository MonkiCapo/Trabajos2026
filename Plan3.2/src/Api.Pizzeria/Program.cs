using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using Api.Pizzeria.Endpoints;
using Api.Pizzeria.Extensions;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.IService;
using Services.Pizzeria.Services;
using Services.Pizzeria.Validations;

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

// Aca configuro qué cadena a conexión se elije en la bd y se inserta los repositorios
builder.Services.AddPizzeriaDatabase(builder.Configuration);

// Registrar servicios de negocio
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IPizzaService, PizzaService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Registrar validadores de FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<PedidoRequestValidator>();

// Registrar OpenAPI (documentación para Scalar)
builder.Services.AddOpenApi();

var app = builder.Build();

// Documentación de la API con Scalar
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("PizzeriaAPI");
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Mapeo modular de Endpoints
app.MapClienteEndpoints();
app.MapPizzaEndpoints();
app.MapPedidoEndpoints();

app.Run();
