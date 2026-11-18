using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Core.Pizzeria.Configuracion;
using Core.Pizzeria.DTOs;

namespace Services.Pizzeria.Services;

public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerarToken(UsuarioResponse usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new("ClienteId", usuario.ClienteId.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new(ClaimTypes.Name, usuario.Nombre)
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiraEnMinutos),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
