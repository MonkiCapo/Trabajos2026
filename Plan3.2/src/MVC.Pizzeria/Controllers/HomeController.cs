using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Productos()
    {
        var pizzas = new List<PizzaViewModel>
        {
            new PizzaViewModel
            {
                Id = 1,
                Nombre = "Pizza Pepperoni",
                Tamanio = "Grande",
                Precio = 1500.00m,
                Descripcion = "Queso muzzarella, salsa de tomate y abundante pepperoni.",
                Ingredientes = new List<string> { "Muzzarella", "Pepperoni", "Salsa de tomate", "Orégano" }
            },
            new PizzaViewModel
            {
                Id = 2,
                Nombre = "Pizza Jamón y Queso",
                Tamanio = "Grande",
                Precio = 1400.00m,
                Descripcion = "Queso muzzarella, jamón cocido y aceitunas.",
                Ingredientes = new List<string> { "Muzzarella", "Jamón cocido", "Salsa de tomate", "Aceitunas" }
            },
            new PizzaViewModel
            {
                Id = 3,
                Nombre = "Pizza Muzzarella",
                Tamanio = "Grande",
                Precio = 1200.00m,
                Descripcion = "Doble queso muzzarella, salsa de tomate y orégano.",
                Ingredientes = new List<string> { "Muzzarella", "Salsa de tomate", "Orégano", "Aceitunas" }
            },
            new PizzaViewModel
            {
                Id = 4,
                Nombre = "Pizza Napolitana",
                Tamanio = "Grande",
                Precio = 1300.00m,
                Descripcion = "Queso muzzarella, rodajas de tomate, ajo y albahaca fresca.",
                Ingredientes = new List<string> { "Muzzarella", "Salsa de tomate", "Tomate en rodajas", "Ajo", "Albahaca" }
            }
        };

        return View(pizzas);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
