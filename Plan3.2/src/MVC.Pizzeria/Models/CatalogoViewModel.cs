using Core.Pizzeria.Entidades;

namespace MVC.Pizzeria.Models;

public class CatalogoViewModel
{
    public List<PizzaViewModel> Pizzas { get; set; } = new();
    public List<Tamanio> Tamanios { get; set; } = new();
}
