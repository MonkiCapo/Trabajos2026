using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/catalogo/{id}", async (int id, HttpClient client) =>
{
    try
    {
        var response = await client.GetAsync($"http://localhost:5002/api/productos/{id}");
        var statusCode = (int)response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (contentType == "application/json")
        {
            var data = await response.Content.ReadFromJsonAsync<object>();
            return Results.Json(data, statusCode: statusCode);
        }
        
        var textContent = await response.Content.ReadAsStringAsync();
        return Results.Content(textContent, contentType, statusCode: statusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/usuarios/{id}", async (int id, HttpClient client) =>
{
    try
    {
        var response = await client.GetAsync($"http://localhost:5001/api/usuarios/{id}/saldo");
        var statusCode = (int)response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (contentType == "application/json")
        {
            var data = await response.Content.ReadFromJsonAsync<object>();
            return Results.Json(data, statusCode: statusCode);
        }
        
        var textContent = await response.Content.ReadAsStringAsync();
        return Results.Content(textContent, contentType, statusCode: statusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/checkout", async (CompraRequest pedido, HttpClient client) =>
{
    try
    {
        var response = await client.PostAsJsonAsync("http://localhost:5003/api/checkout", pedido);
        var statusCode = (int)response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (contentType == "application/json")
        {
            var data = await response.Content.ReadFromJsonAsync<object>();
            return Results.Json(data, statusCode: statusCode);
        }
        
        var textContent = await response.Content.ReadAsStringAsync();
        return Results.Content(textContent, contentType, statusCode: statusCode);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run("http://localhost:5000");

public record CompraRequest(int UsuarioId, int ProductoId);
