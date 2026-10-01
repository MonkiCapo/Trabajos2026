namespace MVC.Pizzeria.Models
{
    public class PedidoRegistrado
    {
        public int PedidoId { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class PedidoResumen
    {
        public int PedidoId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CantidadPizzas { get; set; }

        public string Items { get; set; } = string.Empty;
    }

    public class MisPedidosViewModel
    {
        public List<PedidoResumen> Pedidos { get; set; } = new();

        public bool HayPedidos => Pedidos.Count > 0;
    }
}