namespace Core.Pizzeria.DTOs;

/// <summary>
/// Datos completos para cerrar un pedido: los datos de contacto del cliente
/// y las pizzas elegidas con sus cantidades.
/// No incluye password: es un alta de cliente, no un inicio de sesion.
/// </summary>
public class CheckoutRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public List<ItemRequest> Items { get; set; } = new();
}
