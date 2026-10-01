using Microsoft.AspNetCore.Mvc;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;

namespace MVC.Pizzeria.ViewComponents
{

    public class CarritoContadorViewComponent : ViewComponent
    {
        private readonly ICarritoStore _carrito;

        public CarritoContadorViewComponent(ICarritoStore carrito)
        {
            _carrito = carrito;
        }

        public IViewComponentResult Invoke()
        {
            var carrito = _carrito.Obtener();

            return View(new CarritoContadorModel
            {
                CantidadTotal = carrito.CantidadTotal,
                HayItems = carrito.HayItems
            });
        }
    }
}
