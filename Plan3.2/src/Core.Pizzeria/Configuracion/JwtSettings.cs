namespace Core.Pizzeria.Configuracion;

// Configuracion del JWT, bindeada desde la seccion "Jwt" del appsettings.
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiraEnMinutos { get; set; } = 120;
}
