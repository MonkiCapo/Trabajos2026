using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Api.Pizzeria.Endpoints;
using Api.Pizzeria.Extensions;
using Core.Pizzeria.Configuracion;
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
builder.Services.AddScoped<IIngredienteService, IngredienteService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<ITamanioService, TamanioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<TokenService>();

// TimeProvider inyectable: los servicios toman las fechas de aca para poder
// fijarlas en los tests en lugar de usar DateTime.UtcNow directamente.
builder.Services.AddSingleton(TimeProvider.System);

// Registrar validadores de FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<PedidoRequestValidator>();

// Configuracion del JWT: clave, issuer y audiencia desde la seccion "Jwt"
// del appsettings (no toca ConnectionStrings).
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Autenticacion con JWT Bearer + politica Admin por rol
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta la clave JWT en la configuracion (Jwt:Key).")))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// Registrar OpenAPI (documentación para Scalar)
builder.Services.AddOpenApi();

var app = builder.Build();

// Documentacion de la API con Scalar: solo en desarrollo. En produccion
// expondria el esquema completo de los endpoints.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("PizzeriaAPI");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Mapeo modular de Endpoints
app.MapAuthEndpoints();
app.MapClienteEndpoints();
app.MapPizzaEndpoints();
app.MapIngredienteEndpoints();
app.MapPedidoEndpoints();
app.MapTamanioEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
