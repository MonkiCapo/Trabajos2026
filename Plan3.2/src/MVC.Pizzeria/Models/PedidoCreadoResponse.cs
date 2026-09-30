namespace MVC.Pizzeria.Models
{
    /// <summary>
    /// Datos que devuelve POST /api/pedidos/checkout.
    /// </summary>
    public class PedidoCreadoResponse
    {
        public int PedidoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
