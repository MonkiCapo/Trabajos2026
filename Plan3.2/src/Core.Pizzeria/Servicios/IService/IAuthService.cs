using Core.Pizzeria.DTOs;

namespace Core.Pizzeria.Servicios.IService;

public interface IAuthService
{
    Task<UsuarioResponse> RegistrarAsync(RegistroRequest request);
    Task<UsuarioResponse?> LoginAsync(LoginRequest request);
}
