using System;

namespace Core.Pizzeria.Entidades;

public class HistorialEstadoPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Servicios.Enum.EstadoPedido Estado { get; set; }
    public DateTime FechaCambio { get; set; }
    public string? Observacion { get; set; }
}
