namespace Api.Pizzeria.DTOs;

public class PedidoRequest
{
    public int ClienteId { get; set; }
    public List<ItemRequest> Items { get; set; } = new();
}
