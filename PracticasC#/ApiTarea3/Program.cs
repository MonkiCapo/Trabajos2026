using System.Net;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapPost("/api/checkout", async (CompraRequest pedido, HttpClient client) =>
{
    try
    {
        var responseCatalogo = await client.GetAsync($"http://localhost:5002/api/productos{pedido.ProductoId}");
        if (!responseCatalogo.IsSuccessStatusCode)
            return Results.BadRequest(new {Error = "El producto no existe en el catálogo."});

        var producto = await responseCatalogo.Content.ReadFromJsonAsync<ProductoDTO>();

        var responseUsuarios = await client.GetAsync($"http://localhost:5001/api/usuarios/{pedido.UsuarioId}/saldo");
        if (!responseCatalogo.IsSuccessStatusCode)
            return Results.BadRequest(new {Error = "El usuario no existe." });

        var usuario = await responseUsuarios.Content.ReadFromJsonAsync<UsuarioDTO>();

        if (usuario.Saldo >= producto.Precio)
        {
            return Results.Ok(new
            {
                Estado = "Aprobado",
                Mensaje = $"Compra exitosa. Se debitaron ${producto.Precio} de la cuenta del usuario {pedido.UsuarioId}"
            });
        }
        else
        {
            return Results.BadRequest(new { Estado = "Rechazado", Motivo = "Saldo insuficiente."});
        }
    }
    catch (HttpRequestException ex)
    {
        return Results.Json(new
        {
            Estado = "Error 503 (Servicio Unavaible)",
            Motivo = "Uno de los microservicios internos no responde. Intente Más Tarde",
            DetalleTecnico = ex.Message
        }, statusCode: 503);
    }
});

app.Run("http://localhost:5003");

public record CompraRequest(int UsuarioId, int ProductoId);
public record ProductoDTO(int ProductoId, decimal Precio);
public record UsuarioDTO(int UsuarioId, decimal Saldo);