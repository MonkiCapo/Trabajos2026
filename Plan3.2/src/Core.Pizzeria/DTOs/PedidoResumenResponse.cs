using System;
using System.Text.Json.Serialization;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.DTOs;

public class PedidoResumenResponse
{
    public int PedidoId { get; set; }
    public int ClienteId { get; set; }
    public EstadoPedido Estado { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("ultimaActualizacion")]
    public DateTime FechaActualizacion { get; set; }
}