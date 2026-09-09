using System;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.Entidades;

public class HistorialEstadoPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public EstadoPedido Estado { get; set; }
    public DateTime FechaCambio { get; set; }
    public string? Observacion { get; set; }
}
