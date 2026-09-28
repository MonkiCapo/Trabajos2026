using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IUsuarioRepositorio
{
    Task<int> CrearUsuarioAsync(Usuario usuario);
    Task<Usuario?> ObtenerPorEmailAsync(string email);
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<bool> ExisteEmailAsync(string email);
}
