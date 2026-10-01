namespace Core.Pizzeria.DTOs;

// DTO para registrarse
public class RegistroRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
