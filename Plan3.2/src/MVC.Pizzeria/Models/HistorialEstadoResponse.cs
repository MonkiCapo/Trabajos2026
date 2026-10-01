namespace MVC.Pizzeria.Models
{
    /// <summary>
    /// Un cambio de estado dentro del historial, tal cual lo devuelve
    /// GET /api/pedidos/{id}/historial.
    /// </summary>
    public class HistorialEstadoResponse
    {
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
        public string? Observacion { get; set; }
    }

    /// <summary>
    /// Modelo de la pantalla de seguimiento: el pedido (GET /api/pedidos/{id})
    /// mas su linea de tiempo (GET /api/pedidos/{id}/historial).
    /// </summary>
    public class SeguimientoViewModel
    {
        public PedidoResponse Pedido { get; set; } = new();
        public List<HistorialEstadoResponse> Historial { get; set; } = new();

        /// <summary>Ultimo estado registrado. Es el que se muestra arriba.</summary>
        public HistorialEstadoResponse? EstadoActual => Historial.Count == 0 ? null : Historial[^1];
    }
}