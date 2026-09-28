using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Productos()
    {
        var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5183";
        var client = _httpClientFactory.CreateClient();

        List<Pizza>? pizzas = null;
        try
        {
            pizzas = await client.GetFromJsonAsync<List<Pizza>>($"{baseUrl}/api/pizzas");
        }
        catch (Exception)
        {
            pizzas = new List<Pizza>();
        }

        var listaViewModel = (pizzas ?? new List<Pizza>()).Select(p => new PizzaViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Tamanio = p.Tamanio,
            Precio = p.Precio,
            Ingredientes = p.Ingredientes,
            ImagenUrl = $"/images/pizza-{p.Id}.jpg"
        }).ToList();

        return View(listaViewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
