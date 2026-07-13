namespace Api.Pizzeria.DTOs;

public class ItemRequest
{
    public string PizzaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}
