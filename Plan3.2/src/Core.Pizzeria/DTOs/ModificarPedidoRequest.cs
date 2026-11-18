namespace Core.Pizzeria.DTOs;

public class ModificarPedidoRequest
{
    public List<ItemRequest> Items { get; set; } = new();
}
