namespace MVC.Pizzeria.Models
{
    public class PedidoResponse
    {
        public int PedidoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime UltimaActualizacion { get; set; }
        public PedidoClienteResponse? Cliente { get; set; }
        public List<ItemPedidoResponse> Items { get; set; } = new();
    }

    public class PedidoClienteResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ItemPedidoResponse
    {
        public string Pizza { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
