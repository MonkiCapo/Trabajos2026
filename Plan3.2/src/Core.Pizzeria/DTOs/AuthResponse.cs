namespace Core.Pizzeria.DTOs;

// Lo que la API devuelve despues de un login/registro exitoso: el token JWT
// y los datos del usuario (sin exponer el hash de la contraseña).
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UsuarioResponse Usuario { get; set; } = new();
}
