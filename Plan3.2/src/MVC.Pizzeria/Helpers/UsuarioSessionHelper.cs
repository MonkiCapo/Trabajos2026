using System.Text.Json;
using Core.Pizzeria.DTOs;

namespace MVC.Pizzeria.Helpers;

public static class UsuarioSessionHelper
{
    private const string CLAVE_USUARIO = "UsuarioAutenticado";
    private const string CLAVE_TOKEN = "TokenJwt";

    public static void GuardarUsuario(ISession session, UsuarioResponse usuario)
    {
        var json = JsonSerializer.Serialize(usuario);
        session.SetString(CLAVE_USUARIO, json);
    }

    public static UsuarioResponse? ObtenerUsuario(ISession session)
    {
        var json = session.GetString(CLAVE_USUARIO);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<UsuarioResponse>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void EliminarUsuario(ISession session)
    {
        session.Remove(CLAVE_USUARIO);
    }

    public static void GuardarToken(ISession session, string token)
    {
        session.SetString(CLAVE_TOKEN, token);
    }

    public static string? ObtenerToken(ISession session)
    {
        return session.GetString(CLAVE_TOKEN);
    }

    public static void EliminarToken(ISession session)
    {
        session.Remove(CLAVE_TOKEN);
    }

    public static bool EstaAutenticado(ISession session)
    {
        return !string.IsNullOrWhiteSpace(session.GetString(CLAVE_USUARIO));
    }
}
