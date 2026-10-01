using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios
{
    /// <summary>
    /// Recordatorio de los pedidos que hizo este navegador.
    ///
    /// Existe porque no hay login: sin identificacion del cliente no hay forma
    /// de saber a que pedidos tiene derecho a ver cada persona. Guardar los ids
    /// en la sesion es el compromiso mas simple que no exige autenticacion.
    ///
    /// OJO: es local a un navegador. Si el usuario borra cookies o cambia de
    /// dispositivo, la lista se pierde. Para algo durable haria falta buscar
    /// por email desde la API.
    /// </summary>
    public interface IMisPedidosStore
    {
        /// <summary>Ids de los pedidos, del mas nuevo al mas viejo.</summary>
        IReadOnlyList<PedidoRegistrado> Obtener();

        /// <summary>
        /// Agrega un pedido al principio de la lista. No duplica si ya estaba
        /// y descarta los mas viejos si se pasa del tope.
        /// </summary>
        void Registrar(int pedidoId);

        void Quitar(int pedidoId);

        void Vaciar();
    }
}