namespace Core.Pizzeria.Servicios;

// Utilidad para logs: los emails son datos personales y no deben quedar
// completos en los archivos de log de produccion.
public static class Pii
{
    public static string AnonimizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "(sin email)";
        }

        var arroba = email.IndexOf('@');
        var parteLocal = arroba >= 0 ? email[..arroba] : email;
        var dominio = arroba >= 0 ? email[arroba..] : string.Empty;

        var inicial = parteLocal.Length > 0 ? parteLocal[..1] : string.Empty;
        return $"{inicial}***{dominio}";
    }
}
