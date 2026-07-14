namespace Core.Pizzeria.Entidades;

public class Pizza
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tamanio { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string? Descripcion { get; set; }
}
