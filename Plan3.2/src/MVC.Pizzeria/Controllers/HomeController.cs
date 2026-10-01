using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;
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
    /// Catalogo de pizzas. Agregar al carrito vive en PedidoController.
    /// </summary>
    public async Task<IActionResult> Productos()
    {
        List<Pizza> pizzas;

        try
        {
            var client = _httpClientFactory.CreateClient();
            pizzas = await client.GetFromJsonAsync<List<Pizza>>($"{BaseUrl}/api/pizzas") ?? new List<Pizza>();
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = $"No pudimos conectar con la API ({BaseUrl}). El catalogo esta vacio.";
            pizzas = new List<Pizza>();
        }
        catch (TaskCanceledException)
        {
            ViewData["Error"] = "La API tardo demasiado en responder. El catalogo esta vacio.";
            pizzas = new List<Pizza>();
        }

        var modelo = pizzas.Select(p => new PizzaViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Tamanio = p.Tamanio,
            Precio = p.Precio,
            Ingredientes = p.Ingredientes,
            ImagenUrl = PizzaImagen.UrlDe(p.Id)
        }).ToList();

        return View(modelo);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
