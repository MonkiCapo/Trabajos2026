namespace Core.Pizzeria.DTOs;

// Lo que la API devuelve después de un login/registro exitoso (sin exponer el hash)
public class UsuarioResponse
{
    public int UsuarioId { get; set; }
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
