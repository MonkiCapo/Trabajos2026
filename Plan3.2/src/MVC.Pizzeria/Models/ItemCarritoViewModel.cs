using Core.Pizzeria.Servicios;

namespace MVC.Pizzeria.Models;

public class ItemCarritoViewModel
{
    // Identifica la linea del carrito: dos lineas de la misma pizza con
    // ajustes distintos son dos lineas separadas.
    public string LineaId { get; set; } = Guid.NewGuid().ToString("N");

    public int PizzaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tamanio { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;

    // Precio base de la pizza (sin recargos de extras).
    public decimal Precio { get; set; }
    public int Cantidad { get; set; }

    public List<string> Quitar { get; set; } = new();
    public List<string> Extras { get; set; } = new();

    public bool TieneAjustes => Quitar.Count > 0 || Extras.Count > 0;

    public decimal Recargo => Extras.Count * PreciosAjuste.RecargoExtra;

    public decimal PrecioUnitario => Precio + Recargo;

    public decimal Subtotal => PrecioUnitario * Cantidad;
}
