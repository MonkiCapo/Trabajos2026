using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MVC.Pizzeria.Models;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace MVC.Pizzeria.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private string BaseUrl => _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5183";

    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Catalogo con el formulario de pedido. No hay carrito ni sesion:
    /// el usuario marca las cantidades y completa sus datos en la misma pagina.
    /// </summary>
    public async Task<IActionResult> Productos()
    {
        var client = _httpClientFactory.CreateClient();

        List<Pizza>? pizzas = null;
        try
        {
            pizzas = await client.GetFromJsonAsync<List<Pizza>>($"{BaseUrl}/api/pizzas");
        }
        catch (Exception)
        {
            pizzas = new List<Pizza>();
        }

        var modelo = new CheckoutViewModel
        {
            Pizzas = (pizzas ?? new List<Pizza>()).Select(p => new PizzaViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Tamanio = p.Tamanio,
                Precio = p.Precio,
                Ingredientes = p.Ingredientes,
                ImagenUrl = $"/images/pizza-{p.Id}.jpg",
                Cantidad = 0
            }).ToList()
        };

        return View(modelo);
    }

    /// <summary>
    /// Confirma el pedido: arma el CheckoutRequest y lo envia a la API.
    /// Si la API responde con errores, se los muestra al usuario conservando lo que ya habia cargado.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPedido(CheckoutViewModel model)
    {
        if (model.Pizzas is null)
        {
            model.Pizzas = new List<PizzaViewModel>();
        }

        if (!model.HayItems)
        {
            ModelState.AddModelError(string.Empty, "Elegi al menos una pizza para confirmar el pedido.");
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Productos), model);
        }

        // Solo se envian las pizzas con cantidad mayor a 0
        var request = new CheckoutRequest
        {
            Nombre = model.Nombre,
            Email = model.Email,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Items = model.Pizzas
                .Where(p => p.Cantidad > 0)
                .Select(p => new ItemRequest { PizzaNombre = p.Nombre, Cantidad = p.Cantidad })
                .ToList()
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/pedidos/checkout", request);

            if (!response.IsSuccessStatusCode)
            {
                // Se traducen los errores de la API a ModelState
                foreach (var (clave, mensaje) in await LeerErroresApiAsync(response))
                {
                    ModelState.AddModelError(clave, mensaje);
                }
                return View(nameof(Productos), model);
            }

            var creado = await response.Content.ReadFromJsonAsync<PedidoCreadoResponse>();

            // PRG: se redirige por id, sin pasar estado. La pagina de confirmacion
            // vuelve a consultar la API, asi que el F5 no re-postea el pedido.
            return RedirectToAction(nameof(PedidoConfirmado), new { id = creado?.PedidoId ?? 0 });
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty,
                "No pudimos conectar con la API. Verifica que este corriendo en " + BaseUrl);
            return View(nameof(Productos), model);
        }
    }

    /// <summary>
    /// Muestra el pedido recien confirmado. Vuelve a pedirlo a la API para que
    /// los datos mostrados sean los reales y no los que envio el formulario.
    /// </summary>
    public async Task<IActionResult> PedidoConfirmado(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Productos));
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/pedidos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Productos));
            }

            var pedido = await response.Content.ReadFromJsonAsync<PedidoResponse>();

            if (pedido is null)
            {
                return RedirectToAction(nameof(Productos));
            }

            return View(pedido);
        }
        catch (Exception)
        {
            return RedirectToAction(nameof(Productos));
        }
    }

    /// <summary>
    /// Traduce los distintos formatos de error de la API a una lista de (clave, mensaje).
    /// </summary>
    private static async Task<List<(string Clave, string Mensaje)>> LeerErroresApiAsync(HttpResponseMessage response)
    {
        var errores = new List<(string, string)>();
        var cuerpo = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            errores.Add((string.Empty, "No se pudo completar el pedido."));
            return errores;
        }

        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            var root = doc.RootElement;

            // Formato ValidationProblem: { "errors": { "Email": ["mensaje"] } }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in errors.EnumerateObject())
                {
                    // Las claves con indice (Items[0].Cantidad) no existen en el model del MVC
                    var clave = campo.Name.Contains('[') ? string.Empty : campo.Name;

                    foreach (var mensaje in campo.Value.EnumerateArray())
                    {
                        errores.Add((clave, mensaje.GetString() ?? "Dato invalido."));
                    }
                }

                return errores;
            }

            // Formato { "error": "..." } o { "error": "...", "detalles": "..." }
            if (root.TryGetProperty("error", out var error))
            {
                var mensaje = error.GetString() ?? "Ocurrio un error.";

                if (root.TryGetProperty("detalles", out var detalles) && detalles.ValueKind == JsonValueKind.String)
                {
                    mensaje = $"{mensaje} {detalles.GetString()}";
                }

                errores.Add((string.Empty, mensaje));
            }
        }
        catch (JsonException)
        {
            errores.Add((string.Empty, "La API respondio con un error inesperado."));
        }

        if (errores.Count == 0)
        {
            errores.Add((string.Empty, $"No se pudo completar el pedido. La API respondio {(int)response.StatusCode}."));
        }

        return errores;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
