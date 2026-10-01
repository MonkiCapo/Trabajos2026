namespace Core.Pizzeria.DTOs;

public class CheckoutRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public List<ItemRequest> Items { get; set; } = new();
}
