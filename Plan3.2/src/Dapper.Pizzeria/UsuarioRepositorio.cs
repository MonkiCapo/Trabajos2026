using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class UsuarioRepositorio : DapperRepo, IUsuarioRepositorio
{
    public UsuarioRepositorio(IAdo ado) : base(ado) { }

    public async Task<int> CrearUsuarioAsync(Usuario usuario, IDbConnection? conexion = null, IDbTransaction? transaction = null)
    {
        var sql = "INSERT INTO USUARIO (cliente_id, email, password_hash, rol, fecha_creacion) VALUES (@ClienteId,@Email, @PasswordHash,@Rol,@FechaCreacion); SELECT LAST_INSERT_ID();";

        var parametros = new
        {
            usuario.ClienteId,
            usuario.Email,
            usuario.PasswordHash,
            usuario.Rol,
            FechaCreacion = usuario.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss")
        };

        if (conexion is not null)
        {
            return await conexion.ExecuteScalarAsync<int>(sql, parametros, transaction);
        }

        using var propia = NuevaConexion();
        return await propia.ExecuteScalarAsync<int>(sql, parametros, transaction);
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        var sql = @"SELECT id,
                           cliente_id AS ClienteId,
                           email AS Email,
                           password_hash AS PasswordHash,
                           rol AS Rol,
                           fecha_creacion AS FechaCreacion
                    FROM USUARIO WHERE email = @Email;";

        using var conexion = NuevaConexion();

        return await conexion.QueryFirstOrDefaultAsync<Usuario>(sql, new { Email = email });
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        var sql = @"SELECT id,
                           cliente_id AS ClienteId,
                           email AS Email,
                           password_hash AS PasswordHash,
                           rol AS Rol,
                           fecha_creacion AS FechaCreacion
                    FROM USUARIO WHERE id = @Id;";

        using var conexion = NuevaConexion();

        return await conexion.QueryFirstOrDefaultAsync<Usuario>(sql, new { Id = id });
    }

    public async Task<bool> ExisteEmailAsync(string email)
    {
        var sql = "SELECT COUNT(1) FROM USUARIO WHERE email = @Email";

        using var conexion = NuevaConexion();

        var count = await conexion.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }
}
