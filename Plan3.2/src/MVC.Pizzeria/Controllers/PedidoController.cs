using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace MVC.Pizzeria.Controllers;

public class PedidoController : Controller
{
    private readonly ICarritoStore _carrito;
    private readonly IMisPedidosStore _misPedidos;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public PedidoController(ICarritoStore carrito, IMisPedidosStore misPedidos, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _carrito = carrito;
        _misPedidos = misPedidos;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private string BaseUrl => _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5183";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int pizzaId, int cantidad = 1)
    {
        if (cantidad < 1)
        {
            cantidad = 1;
        }

        try
        {
            var pizza = await ObtenerPizzaDelCatalogoAsync(pizzaId);

            if (pizza is null)
            {
                TempData["Error"] = "Esa pizza no existe en el catalogo.";
                return RedirectToAction("Productos", "Home");
            }

            _carrito.Agregar(new ItemCarritoViewModel
            {
                PizzaId = pizza.Id,
                Nombre = pizza.Nombre,
                Tamanio = pizza.Tamanio,
                Precio = pizza.Precio,
                ImagenUrl = PizzaImagen.UrlDe(pizza.Id)
            }, cantidad);

            TempData["Ok"] = $"Agregamos {cantidad} × {pizza.Nombre} al carrito.";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = $"No pudimos conectar con la API ({BaseUrl}). No pudimos agregar la pizza.";
        }
        catch (TaskCanceledException)
        {
            TempData["Error"] = "La API tardo demasiado en responder. Intenta de nuevo.";
        }

        return RedirectToAction("Productos", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Actualizar(int pizzaId, int cantidad)
    {
        _carrito.Actualizar(pizzaId, cantidad);
        return RedirectToAction(nameof(Resumen));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Quitar(int pizzaId)
    {
        _carrito.Quitar(pizzaId);
        return RedirectToAction(nameof(Resumen));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Vaciar()
    {
        _carrito.Vaciar();
        return RedirectToAction("Productos", "Home");
    }

    public IActionResult Resumen()
    {
        // El carrito vive en la sesion: esta pantalla no necesita la API.
        return View(_carrito.Obtener());
    }

    public IActionResult Confirmacion()
    {
        var carrito = _carrito.Obtener();

        if (!carrito.HayItems)
        {
            TempData["Error"] = "Tu carrito esta vacio. Agrega al menos una pizza.";
            return RedirectToAction("Productos", "Home");
        }

        return View(new ConfirmacionViewModel { Carrito = carrito });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(ConfirmacionViewModel model)
    {
        // El carrito manda: si no hay items no hay pedido que confirmar.
        var carrito = _carrito.Obtener();

        if (!carrito.HayItems)
        {
            TempData["Error"] = "Tu carrito esta vacio. Agrega al menos una pizza.";
            return RedirectToAction("Productos", "Home");
        }

        // Se recarga para que la vista pueda volver a pintar el resumen
        // aunque falle la validacion o la API.
        model.Carrito = carrito;

        if (!ModelState.IsValid)
        {
            return View(nameof(Confirmacion), model);
        }

        // Los items salen de la SESION, nunca del formulario: un POST
        // manipulado no puede meter pizzas que el usuario no agrego.
        // Y la API igual recalcula cada precio desde PIZZA antes de cobrar.
        var request = new CheckoutRequest
        {
            Nombre = model.Nombre,
            Email = model.Email,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Items = carrito.Items
                .Select(i => new ItemRequest { PizzaNombre = i.Nombre, Cantidad = i.Cantidad })
                .ToList()
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/pedidos/checkout", request);

            if (!response.IsSuccessStatusCode)
            {
                foreach (var (clave, mensaje) in await ApiErrorReader.LeerAsync(response))
                {
                    ModelState.AddModelError(clave, mensaje);
                }
                return View(nameof(Confirmacion), model);
            }

            var creado = await response.Content.ReadFromJsonAsync<PedidoCreadoResponse>();
            var pedidoId = creado?.PedidoId ?? 0;

            if (pedidoId <= 0)
            {
                ModelState.AddModelError(string.Empty,
                    "La API confirmo el pedido pero no devolvio el numero de pedido.");
                return View(nameof(Confirmacion), model);
            }

            // El pedido ya quedo persistido en PEDIDO + ITEM_PEDIDO:
            // el carrito cumplio su funcion.
            _carrito.Vaciar();

            // Y se deja anotado en la sesion para poder volver a encontrarlo
            // desde "Mis pedidos", sin login.
            _misPedidos.Registrar(pedidoId);

            TempData["Ok"] = "Tu pedido fue confirmado.";

            // PRG: se redirige por id y esta pantalla vuelve a consultar la
            // API, asi que el F5 no re-postea el pedido.
            return RedirectToAction(nameof(Confirmado), new { id = pedidoId });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty,
                "No pudimos conectar con la API. Verifica que este corriendo en " + BaseUrl);
            return View(nameof(Confirmacion), model);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, "La API tardo demasiado en responder. Intenta de nuevo.");
            return View(nameof(Confirmacion), model);
        }
    }

    public async Task<IActionResult> Confirmado(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction("Productos", "Home");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/pedidos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return RedirectToAction("Productos", "Home");
            }

            var pedido = await response.Content.ReadFromJsonAsync<PedidoResponse>();

            if (pedido is null)
            {
                return RedirectToAction("Productos", "Home");
            }

            return View(pedido);
        }
        catch (HttpRequestException)
        {
            return RedirectToAction("Productos", "Home");
        }
        catch (TaskCanceledException)
        {
            return RedirectToAction("Productos", "Home");
        }
    }


    public async Task<IActionResult> MisPedidos()
    {
        var guardados = _misPedidos.Obtener();

        if (guardados.Count == 0)
        {
            return View(new MisPedidosViewModel());
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            async Task<(PedidoResumen? Resumen, PedidoRegistrado Guardado)> Cargar(PedidoRegistrado guardado)
            {
                var respuesta = await client.GetAsync($"{BaseUrl}/api/pedidos/{guardado.PedidoId}");

                if (!respuesta.IsSuccessStatusCode)
                {
                    return (null, guardado);
                }

                var pedido = await respuesta.Content.ReadFromJsonAsync<PedidoResponse>();

                if (pedido is null)
                {
                    return (null, guardado);
                }

                return (new PedidoResumen
                {
                    PedidoId = pedido.PedidoId,
                    FechaCreacion = pedido.FechaCreacion,
                    Estado = pedido.Estado,
                    Total = pedido.Total,
                    Cliente = pedido.Cliente?.Nombre ?? "-",
                    Email = pedido.Cliente?.Email ?? "-",
                    CantidadPizzas = pedido.Items.Sum(i => i.Cantidad),
                    Items = string.Join(", ", pedido.Items.Select(i => $"{i.Cantidad} × {i.Pizza}"))
                }, guardado);
            }

            var cargados = await Task.WhenAll(guardados.Select(Cargar));

            // Pedidos que ya no existen en la API (borrados a mano): se sacan de la sesion para no dejarlos fantasma en la lista.
            foreach (var caido in cargados.Where(c => c.Resumen is null))
            {
                _misPedidos.Quitar(caido.Guardado.PedidoId);
            }

            var lista = cargados
                .Where(c => c.Resumen is not null)
                .Select(c => c.Resumen!)
                .OrderByDescending(p => p.FechaCreacion)
                .ToList();

            return View(new MisPedidosViewModel { Pedidos = lista });
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = $"No pudimos conectar con la API ({BaseUrl}).";
            return View(new MisPedidosViewModel());
        }
        catch (TaskCanceledException)
        {
            TempData["Error"] = "La API tardo demasiado en responder. Intenta de nuevo.";
            return View(new MisPedidosViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult OlvidarPedido(int id)
    {
        if (id > 0)
        {
            _misPedidos.Quitar(id);
            TempData["Ok"] = $"El pedido #{id} ya no aparece en tu lista.";
        }

        return RedirectToAction(nameof(MisPedidos));
    }

    public async Task<IActionResult> Seguimiento(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction("Productos", "Home");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            var pedidoResponse = await client.GetAsync($"{BaseUrl}/api/pedidos/{id}");
            if (!pedidoResponse.IsSuccessStatusCode)
            {
                return RedirectToAction("Productos", "Home");
            }

            var pedido = await pedidoResponse.Content.ReadFromJsonAsync<PedidoResponse>();
            if (pedido is null)
            {
                return RedirectToAction("Productos", "Home");
            }

            var historialResponse = await client.GetAsync($"{BaseUrl}/api/pedidos/{id}/historial");
            if (!historialResponse.IsSuccessStatusCode)
            {
                return RedirectToAction("Productos", "Home");
            }

            // La API ya devuelve el historial de mas viejo a mas nuevo y la vista
            // lo pinta en ese orden, asi que no hay que reordenar nada aca.
            var historial =
                await historialResponse.Content.ReadFromJsonAsync<List<HistorialEstadoResponse>>()
                ?? new List<HistorialEstadoResponse>();

            return View(new SeguimientoViewModel { Pedido = pedido, Historial = historial });
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = $"No pudimos conectar con la API ({BaseUrl}).";
            return RedirectToAction("Productos", "Home");
        }
        catch (TaskCanceledException)
        {
            TempData["Error"] = "La API tardo demasiado en responder. Intenta de nuevo.";
            return RedirectToAction("Productos", "Home");
        }
    }

    private async Task<Pizza?> ObtenerPizzaDelCatalogoAsync(int pizzaId)
    {
        var client = _httpClientFactory.CreateClient();
        var pizzas = await client.GetFromJsonAsync<List<Pizza>>($"{BaseUrl}/api/pizzas");

        return pizzas?.FirstOrDefault(p => p.Id == pizzaId);
    }
}
