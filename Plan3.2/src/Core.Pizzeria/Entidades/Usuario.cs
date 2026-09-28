namespace Core.Pizzeria.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Cliente";
    public DateTime FechaCreacion { get; set; }
}
