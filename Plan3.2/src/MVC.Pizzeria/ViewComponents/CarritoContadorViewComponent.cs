using Microsoft.AspNetCore.Mvc;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;

namespace MVC.Pizzeria.ViewComponents
{
    /// <summary>
    /// Muestra cuantas pizzas tiene el usuario en el carrito. Va como
    /// ViewComponent y no con @inject en el layout porque el estado del
    /// carrito no pertenece a la vista: se resuelve desde la sesion.
    /// </summary>
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
