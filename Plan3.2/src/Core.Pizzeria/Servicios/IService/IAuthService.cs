using Core.Pizzeria.DTOs;

namespace Core.Pizzeria.Servicios.IService;

public interface IAuthService
{
    Task<AuthResponse> RegistrarAsync(RegistroRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<UsuarioResponse?> ObtenerPerfilAsync(int usuarioId);
}
