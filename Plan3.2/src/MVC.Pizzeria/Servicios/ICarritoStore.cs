using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios
{
    public interface ICarritoStore
    {
        CarritoViewModel Obtener();

        void Guardar(CarritoViewModel carrito);

        void Agregar(ItemCarritoViewModel item, int cantidad);

        void Actualizar(int pizzaId, int cantidad);

        void Quitar(int pizzaId);

        void Vaciar();
    }
}
