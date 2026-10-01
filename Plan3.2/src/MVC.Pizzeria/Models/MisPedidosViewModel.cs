namespace MVC.Pizzeria.Models
{
    /// <summary>
    /// Lo que el MVC guarda en la sesion para saber que pedidos son tuyos.
    ///
    /// Solo se guarda el id y la fecha: el estado, el total y los items se
    /// leen siempre de la API, porque un estado guardado en la sesion queda
    /// viejo en cuanto el pedido avanza.
    /// </summary>
    public class PedidoRegistrado
    {
        public int PedidoId { get; set; }
        public DateTime Fecha { get; set; }
    }

    /// <summary>
    /// Una fila de la lista de "mis pedidos", con los datos que devuelve
    /// GET /api/pedidos/{id}.
    /// </summary>
    public class PedidoResumen
    {
        public int PedidoId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CantidadPizzas { get; set; }

        /// <summary>Detalle legible: "2 × Muzzarella, 1 × Pepperoni".</summary>
        public string Items { get; set; } = string.Empty;
    }

    public class MisPedidosViewModel
    {
        public List<PedidoResumen> Pedidos { get; set; } = new();

        public bool HayPedidos => Pedidos.Count > 0;
    }
}