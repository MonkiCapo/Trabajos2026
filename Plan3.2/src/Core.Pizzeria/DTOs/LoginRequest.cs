namespace Core.Pizzeria.DTOs;

// Lo que el usuario manda para iniciar sesión
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
