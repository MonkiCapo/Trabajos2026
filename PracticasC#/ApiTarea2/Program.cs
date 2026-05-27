var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var productos = new Dictionary<int, ProductoInfo>
{
    {10, new ProductoInfo(800.00m, 2)}, // Pizza Muzarella (inicializado con 2 para pruebas)
    {20, new ProductoInfo(1200.00m, 10)}
};

app.MapGet("/api/productos/{id}", (int id) =>
{
    if (productos.TryGetValue(id, out var prod))
    {
        return Results.Ok(new { ProductoId = id, Precio = prod.Precio, Stock = prod.Stock });
    }
    return Results.NotFound(new { Mensaje = "Producto sin stock o inexistente" });
});

app.MapPost("/api/productos/{id}/descontar", (int id) =>
{
    if (productos.TryGetValue(id, out var prod))
    {
        if (prod.Stock <= 0)
        {
            return Results.BadRequest("No hay stock");
        }
        prod.Stock -= 1;
        return Results.Ok(new { ProductoId = id, NuevoStock = prod.Stock });
    }
    return Results.NotFound(new { Mensaje = "Producto no encontrado" });
});

app.Run("http://localhost:5002");

public class ProductoInfo
{
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    public ProductoInfo(decimal precio, int stock)
    {
        Precio = precio;
        Stock = stock;
    }
}