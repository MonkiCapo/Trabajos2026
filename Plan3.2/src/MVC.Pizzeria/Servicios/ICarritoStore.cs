using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios;

public interface ICarritoStore
{
    CarritoViewModel Obtener();

    void Guardar(CarritoViewModel carrito);

    void Agregar(ItemCarritoViewModel item, int cantidad);

    void Actualizar(string lineaId, int cantidad);

    void Quitar(string lineaId);

    void Vaciar();
}
