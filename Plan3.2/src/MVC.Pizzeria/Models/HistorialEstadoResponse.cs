namespace MVC.Pizzeria.Models
{
    public class HistorialEstadoResponse
    {
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
        public string? Observacion { get; set; }
    }

    public class SeguimientoViewModel
    {
        public PedidoResponse Pedido { get; set; } = new();
        public List<HistorialEstadoResponse> Historial { get; set; } = new();

        public HistorialEstadoResponse? EstadoActual => Historial.Count == 0 ? null : Historial[^1];
    }
}