using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.DTOs;

public class PedidoDetalleResponse
{
    public int PedidoId { get; set; }
    public EstadoPedido Estado { get; set; }
    public ClienteResumenResponse? Cliente { get; set; }
    public List<ItemPedidoResponse> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("ultimaActualizacion")]
    public DateTime FechaActualizacion { get; set; }
}