using System.Net.Http.Headers;
using MVC.Pizzeria.Helpers;

namespace MVC.Pizzeria.Servicios;

// Agrega Authorization: Bearer a cada llamada a la API con el token JWT
// guardado en la sesion del usuario logueado. Sin sesion (invitado), las
// llamadas salen sin header y solo alcanzan los endpoints publicos.
public class JwtSessionHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtSessionHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var token = session is null ? null : UsuarioSessionHelper.ObtenerToken(session);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
