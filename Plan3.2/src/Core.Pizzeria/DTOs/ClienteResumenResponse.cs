namespace Core.Pizzeria.DTOs;

public class ClienteResumenResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}