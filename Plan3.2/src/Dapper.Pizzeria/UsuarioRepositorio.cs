using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Entidades;
using System.Data;
using Dapper;

namespace Dapper.Pizzeria;
public class UsuarioRepositorio : DapperRepo, IUsuarioRepositorio
{
    public UsuarioRepositorio(IAdo _ado) : base(_ado){}

    public async Task<int> CrearUsuarioAsync(Usuario usuario, IDbConnection? conexion = null, IDbTransaction? transaction = null)
    {
        var sql = "INSERT INTO USUARIO (cliente_id, email, password_hash, rol, fecha_creacion) VALUES (@ClienteId,@Email, @PasswordHash,@Rol,@FechaCreacion); SELECT LAST_INSERT_ID();";

        var conn = conexion ?? Conexion;

        return await conn.ExecuteScalarAsync<int>(sql, new {
            usuario.ClienteId,
            usuario.Email,
            usuario.PasswordHash,
            usuario.Rol,
            FechaCreacion = usuario.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss")
        }, transaction);
    }

    public Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        var sql = @"SELECT id,
                           cliente_id AS ClienteId,
                           email AS Email,
                           password_hash AS PasswordHash,
                           rol AS Rol,
                           fecha_creacion AS FechaCreacion
                    FROM USUARIO WHERE email = @Email;";
        return Conexion.QueryFirstOrDefaultAsync<Usuario>(sql, new { Email = email });
    }

    public Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        var sql = @"SELECT id,
                           cliente_id AS ClienteId,
                           email AS Email,
                           password_hash AS PasswordHash,
                           rol AS Rol,
                           fecha_creacion AS FechaCreacion
                    FROM USUARIO WHERE id = @Id;";
        return Conexion.QueryFirstOrDefaultAsync<Usuario>(sql, new { Id = id });
    }

    public async Task<bool> ExisteEmailAsync(string email)
    {
        var sql = "SELECT COUNT(1) FROM USUARIO WHERE email = @Email;";
        var count = await Conexion.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }

}
