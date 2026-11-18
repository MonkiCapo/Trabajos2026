using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Core.Pizzeria.Entidades;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;

namespace MVC.Pizzeria.Controllers;

public class HomeController : Controller
{
    private readonly PizzeriaApiClient _api;

    public HomeController(PizzeriaApiClient api)
    {
        _api = api;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Productos()
    {
        var resultado = await _api.ObtenerPizzasAsync();

        if (!resultado.Exito)
        {
            ViewData["Error"] = $"{resultado.Errores[0].Mensaje} El catalogo esta vacio.";
            return View(new CatalogoViewModel());
        }

        var pizzas = resultado.Datos!
            .Select(p => new PizzaViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Tamanio = p.Tamanio,
                Precio = p.Precio,
                Ingredientes = p.Ingredientes,
                ImagenUrl = PizzaImagen.UrlDe(p.Id)
            })
            .ToList();

        // Los tamanios alimentan el selector de cada tarjeta. Si la API no
        // responde, el catalogo igual se muestra con el precio base (Grande):
        // la API vuelve a validar el tamanio al confirmar el pedido.
        var tamaniosResultado = await _api.ObtenerTamaniosAsync();
        var tamanios = tamaniosResultado.Exito && tamaniosResultado.Datos is { Count: > 0 }
            ? tamaniosResultado.Datos
            : new List<Tamanio> { new() { Id = 1, Nombre = "Grande", Multiplicador = 1m } };

        return View(new CatalogoViewModel { Pizzas = pizzas, Tamanios = tamanios });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
