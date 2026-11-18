using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios;

// Cliente tipado para la API (AddHttpClient<PizzeriaApiClient>): todos los
// controladores hablan con la API a traves de aca, con el BaseAddress, el
// timeout y la serializacion configurados en un unico lugar.
public class PizzeriaApiClient
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public PizzeriaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private string BaseUrl => _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "la API";

    public Task<ApiResult<List<Pizza>>> ObtenerPizzasAsync() =>
        ObtenerAsync<List<Pizza>>("/api/pizzas");

    public Task<ApiResult<List<Ingrediente>>> ObtenerIngredientesAsync() =>
        ObtenerAsync<List<Ingrediente>>("/api/ingredientes");

    public Task<ApiResult<Pizza>> CrearPizzaAsync(PizzaRequest request) =>
        EnviarAsync<Pizza>(HttpMethod.Post, "/api/pizzas", request);

    public Task<ApiResult<Pizza>> ActualizarPizzaAsync(int id, PizzaRequest request) =>
        EnviarAsync<Pizza>(HttpMethod.Put, $"/api/pizzas/{id}", request);

    public Task<ApiResult<object?>> EliminarPizzaAsync(int id) =>
        EliminarAsync($"/api/pizzas/{id}");

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        EnviarAsync<AuthResponse>(HttpMethod.Post, "/api/auth/login", request);

    public Task<ApiResult<AuthResponse>> RegistroAsync(RegistroRequest request) =>
        EnviarAsync<AuthResponse>(HttpMethod.Post, "/api/auth/registro", request);

    public Task<ApiResult<PedidoCreadoResponse>> CheckoutAsync(CheckoutRequest request) =>
        EnviarAsync<PedidoCreadoResponse>(HttpMethod.Post, "/api/pedidos/checkout", request);

    public Task<ApiResult<PedidoResponse>> ObtenerPedidoAsync(int id) =>
        ObtenerAsync<PedidoResponse>($"/api/pedidos/{id}");

    public Task<ApiResult<List<HistorialEstadoResponse>>> ObtenerHistorialAsync(int id) =>
        ObtenerAsync<List<HistorialEstadoResponse>>($"/api/pedidos/{id}/historial");

    public Task<ApiResult<List<Tamanio>>> ObtenerTamaniosAsync() =>
        ObtenerAsync<List<Tamanio>>("/api/tamanios");

    public Task<ApiResult<Tamanio>> ActualizarTamanioAsync(int id, TamanioUpdateRequest request) =>
        EnviarAsync<Tamanio>(HttpMethod.Put, $"/api/tamanios/{id}", request);

    private async Task<ApiResult<T>> ObtenerAsync<T>(string ruta)
    {
        try
        {
            using var respuesta = await _httpClient.GetAsync(ruta);
            return await ProcesarAsync<T>(respuesta);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.FallaDeConexion($"No pudimos conectar con la API ({BaseUrl}).");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.FallaDeConexion("La API tardó demasiado en responder. Intenta de nuevo.");
        }
        catch (JsonException)
        {
            return ApiResult<T>.Falla("La API respondio con un formato inesperado.");
        }
    }

    private async Task<ApiResult<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object cuerpo)
    {
        try
        {
            using var mensaje = new HttpRequestMessage(metodo, ruta)
            {
                Content = JsonContent.Create(cuerpo, options: OpcionesJson)
            };
            using var respuesta = await _httpClient.SendAsync(mensaje);
            return await ProcesarAsync<T>(respuesta);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.FallaDeConexion($"No pudimos conectar con la API ({BaseUrl}).");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.FallaDeConexion("La API tardó demasiado en responder. Intenta de nuevo.");
        }
        catch (JsonException)
        {
            return ApiResult<T>.Falla("La API respondio con un formato inesperado.");
        }
    }

    // DELETE: la API responde 204 sin cuerpo, asi que el exito no se parsea.
    private async Task<ApiResult<object?>> EliminarAsync(string ruta)
    {
        try
        {
            using var respuesta = await _httpClient.DeleteAsync(ruta);
            return await ProcesarEliminacionAsync(respuesta);
        }
        catch (HttpRequestException)
        {
            return ApiResult<object?>.FallaDeConexion($"No pudimos conectar con la API ({BaseUrl}).");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<object?>.FallaDeConexion("La API tardó demasiado en responder. Intenta de nuevo.");
        }
    }

    private static async Task<ApiResult<object?>> ProcesarEliminacionAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return ApiResult<object?>.Ok(null);
        }

        var codigoEstado = (int)respuesta.StatusCode;
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(cuerpo))
        {
            return ApiResult<object?>.Falla("Tu sesion vencio o no es valida. Volve a iniciar sesion.", codigoEstado);
        }

        return ApiResult<object?>.Falla(ApiErrorReader.Interpretar(cuerpo, codigoEstado), codigoEstado);
    }

    private static async Task<ApiResult<T>> ProcesarAsync<T>(HttpResponseMessage respuesta)
    {
        var codigoEstado = (int)respuesta.StatusCode;

        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();

            // 401 con cuerpo vacio = el JWT no paso la validacion (vencido o
            // invalido). Si trae cuerpo (ej. login fallido) se parsea normal.
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(cuerpo))
            {
                return ApiResult<T>.Falla("Tu sesion vencio o no es valida. Volve a iniciar sesion.", codigoEstado);
            }

            return ApiResult<T>.Falla(ApiErrorReader.Interpretar(cuerpo, codigoEstado), codigoEstado);
        }

        var datos = await respuesta.Content.ReadFromJsonAsync<T>(OpcionesJson);
        if (datos is null)
        {
            return ApiResult<T>.Falla("La API respondio con un formato inesperado.");
        }

        return ApiResult<T>.Ok(datos);
    }
}
