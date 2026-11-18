namespace MVC.Pizzeria.Models;

public class PedidoResponse
{
    public int PedidoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime UltimaActualizacion { get; set; }
    public PedidoClienteResponse? Cliente { get; set; }
    public List<ItemPedidoResponse> Items { get; set; } = new();
}

public class PedidoClienteResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ItemPedidoResponse
{
    public int PizzaId { get; set; }
    public string Pizza { get; set; } = string.Empty;
    public string Tamanio { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public List<string> Quitar { get; set; } = new();
    public List<string> Extras { get; set; } = new();
    public decimal Subtotal => Cantidad * PrecioUnitario;

    public string TextoAjustes
    {
        get
        {
            var partes = new List<string>();
            if (Quitar.Count > 0) partes.Add("sin " + string.Join(", ", Quitar));
            if (Extras.Count > 0) partes.Add("extra " + string.Join(", ", Extras));
            return string.Join(" · ", partes);
        }
    }
}
