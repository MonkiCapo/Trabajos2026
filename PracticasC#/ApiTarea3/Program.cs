using System.Net;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapPost("/api/checkout", async (CompraRequest pedido, HttpClient client) =>
{
    try
    {
        // 1. Verificar si el producto existe y tiene stock
        var responseCatalogo = await client.GetAsync($"http://localhost:5002/api/productos/{pedido.ProductoId}");
        if (!responseCatalogo.IsSuccessStatusCode)
        {
            return Results.BadRequest(new { Error = "El producto no existe en el catálogo." });
        }

        var producto = await responseCatalogo.Content.ReadFromJsonAsync<ProductoDTO>();
        if (producto == null)
        {
            return Results.BadRequest(new { Error = "Error al leer datos del producto." });
        }

        if (producto.Stock <= 0)
        {
            return Results.BadRequest(new { Error = "No hay stock disponible para este producto." });
        }

        // 2. Descontar stock (asegurar y reservar la unidad)
        var descontarResponse = await client.PostAsync($"http://localhost:5002/api/productos/{pedido.ProductoId}/descontar", null);
        if (!descontarResponse.IsSuccessStatusCode)
        {
            return Results.BadRequest(new { Error = "No se pudo reservar el stock del producto." });
        }

        // 3. Debitar dinero de la cuenta del usuario
        var debitarResponse = await client.PostAsJsonAsync($"http://localhost:5001/api/usuarios/{pedido.UsuarioId}/debitar", new { Monto = producto.Precio });
        if (!debitarResponse.IsSuccessStatusCode)
        {
            // Intentar leer el motivo del fallo del débito
            object? motivoFallo = null;
            try
            {
                motivoFallo = await debitarResponse.Content.ReadFromJsonAsync<object>();
            }
            catch
            {
                motivoFallo = await debitarResponse.Content.ReadAsStringAsync();
            }

            // Error de inconsistencia: el stock se descontó pero no se cobró
            return Results.Json(new
            {
                Estado = "Inconsistente",
                Error = "El sistema quedó en un estado inconsistente. El stock se descontó pero no se pudo realizar el cobro del saldo.",
                Detalle = $"Se descontó una unidad del producto {pedido.ProductoId} pero falló el cobro de ${producto.Precio} al usuario {pedido.UsuarioId}.",
                MotivoDebitoFallido = motivoFallo
            }, statusCode: 500);
        }

        return Results.Ok(new
        {
            Estado = "Aprobado",
            Mensaje = $"Compra exitosa. Se descontaron ${producto.Precio} del usuario {pedido.UsuarioId} y se reservó la unidad."
        });
    }
    catch (HttpRequestException ex)
    {
        return Results.Json(new
        {
            Estado = "Error 503 (Servicio Unavailable)",
            Motivo = "Uno de los microservicios internos no responde. Intente más tarde.",
            DetalleTecnico = ex.Message
        }, statusCode: 503);
    }
});

app.Run("http://localhost:5003");

public record CompraRequest(int UsuarioId, int ProductoId);
public record ProductoDTO(int ProductoId, decimal Precio, int Stock);
public record UsuarioDTO(int UsuarioId, decimal Saldo);