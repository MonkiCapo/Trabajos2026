namespace Core.Pizzeria.DTOs;

public class ItemPedidoResponse
{
    public string Pizza { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}