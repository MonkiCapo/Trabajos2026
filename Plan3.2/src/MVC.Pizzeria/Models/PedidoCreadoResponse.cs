namespace MVC.Pizzeria.Models
{
    public class PedidoCreadoResponse
    {
        public int PedidoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
