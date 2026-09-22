using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVC.Pizzeria.Models;
using Core.Pizzeria.Entidades;
using System.Data.Common;

namespace MVC.Pizzeria.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Productos()
    {
        // Aca creo las pizzas con los atributos originales.
        var pizzasDelCore = new List<Pizza>
        {
            new Pizza { Id = 1, Nombre = "Pizza Pepperoni", Tamanio = "Grande", Precio = 1500.00m, Ingredientes = new() { "Muzzarella", "Pepperoni", "Salsa de tomate" } },
            new Pizza { Id = 2, Nombre = "Pizza Jamón y Queso", Tamanio = "Grande", Precio = 1400.00m, Ingredientes = new() { "Muzzarella", "Salsa de tomate",  } },
            new Pizza { Id = 3, Nombre = "Pizza Muzzarella", Tamanio = "Grande", Precio = 1200.00m, Ingredientes = new() { "Muzzarella", "Salsa de tomate", "Orégano" } },
            new Pizza { Id = 4, Nombre = "Pizza Napolitana", Tamanio = "Grande", Precio = 1300.00m, Ingredientes = new() { "Muzzarella", "Salsa de tomate", "Tomate en rodajas", "Ajo" } }
        };

        var listaViewModel = pizzasDelCore.Select(p => new PizzaViewModel
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
