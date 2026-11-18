namespace Core.Pizzeria.DTOs;

public class PizzaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Tamanio { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public List<string> Ingredientes { get; set; } = new();
}
