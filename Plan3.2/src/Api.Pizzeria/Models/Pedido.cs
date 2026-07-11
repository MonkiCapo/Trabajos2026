using System;
using System.Collections.Generic;

namespace Api.Pizzeria.Models;

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public EstadoPedido Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public decimal Total { get; set; }

    public List<ItemPedido> Items { get; set; } = new();
}
