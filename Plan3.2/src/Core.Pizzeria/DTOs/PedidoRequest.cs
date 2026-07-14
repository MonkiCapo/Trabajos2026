namespace Core.Pizzeria.DTOs;

public class PedidoRequest
{
    public string ClienteEmail { get; set; } = string.Empty;
    public List<ItemRequest> Items { get; set; } = new();
}
