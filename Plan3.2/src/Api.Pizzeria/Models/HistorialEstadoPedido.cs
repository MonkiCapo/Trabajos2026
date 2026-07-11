using System;

namespace Api.Pizzeria.Models;

public class HistorialEstadoPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public EstadoPedido Estado { get; set; }
    public DateTime FechaCambio { get; set; }
    public string? Observacion { get; set; }
}
