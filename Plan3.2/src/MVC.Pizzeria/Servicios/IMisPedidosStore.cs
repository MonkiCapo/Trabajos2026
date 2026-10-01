using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios
{
    public interface IMisPedidosStore
    {
        IReadOnlyList<PedidoRegistrado> Obtener();

        void Registrar(int pedidoId);

        void Quitar(int pedidoId);

        void Vaciar();
    }
}