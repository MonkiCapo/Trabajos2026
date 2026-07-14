namespace Api.Pizzeria.Models;

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int PizzaId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    // Propiedades auxiliares
    public string PizzaNombre { get; set; } = string.Empty;
}
