namespace Core.Pizzeria.Entidades;

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int PizzaId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    // Nombre del tamaño elegido: queda guardado tal cual en el pedido para
    // que el precio historico no dependa de cambios futuros en TAMANIO.
    public string Tamanio { get; set; } = string.Empty;

    // Propiedades auxiliares
    public string PizzaNombre { get; set; } = string.Empty;
    public List<string> Quitar { get; set; } = new();
    public List<string> Extras { get; set; } = new();
}
