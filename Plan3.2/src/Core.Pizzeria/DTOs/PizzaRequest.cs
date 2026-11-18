namespace Core.Pizzeria.DTOs;

public class PizzaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Tamanio { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string? Descripcion { get; set; }
    public int Stock { get; set; }
    public bool Disponible { get; set; } = true;
}