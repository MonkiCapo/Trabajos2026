var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var productos = new Dictionary<int, decimal>
{
    {10, 800.00m}, // Pizza Muzarella
    {20, 1200.00m}
};

app.MapGet("/api/productos/{id}", (int id) =>
{
    if (productos.TryGetValue(id, out var precio))
    {
        return Results.Ok(new {ProductoId = id, Precio = precio});
    }
    return Results.NotFound(new { Mensaje = "Producto sin stock o inexistente"});
});

app.Run("http://localhost:5002");