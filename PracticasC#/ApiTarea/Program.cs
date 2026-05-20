using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var usuarios = new Dictionary<int, decimal>
{
    {1, 1500.00m},
    {2, 200.00m}
};

app.MapGet("/api/usuarios/{id}/saldo", (int id) =>
{
    if (usuarios.TryGetValue(id, out var saldo))
    {
        return Results.Ok(new {UsuarioId = id, Saldo = saldo});
    }
    return Results.NotFound(new {Mensaje = "Usuario no encontrado"});
});

app.MapPost("/api/usuarios/{id}/debitar", (int Cobrarmonto) =>
{
    
});

app.Run("https://localhost:5001");