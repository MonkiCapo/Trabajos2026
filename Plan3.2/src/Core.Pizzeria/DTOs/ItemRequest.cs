namespace Core.Pizzeria.DTOs;

public class ItemRequest
{
    public string PizzaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }

    // Nombre del tamaño elegido (por defecto "Grande"). La API valida que
    // exista y recalcula el precio con su multiplicador.
    public string Tamanio { get; set; } = string.Empty;

    // Ajustes de "Arma tu pizza": nombres de ingredientes.
    // Quitar: ingredientes que trae la pizza y no se quieren.
    // Extras: ingredientes adicionales, cada uno con recargo.
    public List<string> Quitar { get; set; } = new();
    public List<string> Extras { get; set; } = new();
}
