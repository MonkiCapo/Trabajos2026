using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class ClienteRepositorio : DapperRepo, IClienteRepositorio
{
    public ClienteRepositorio(IAdo ado) : base(ado) { }

    public async Task<int> AgregarClienteAsync(Cliente cliente, IDbConnection? conexion = null, IDbTransaction? transaction = null)
    {
        var sql = @"INSERT INTO CLIENTE (nombre, email, telefono, direccion) 
                    VALUES (@Nombre, @Email, @Telefono, @Direccion);
                    SELECT LAST_INSERT_ID();";

        var parametros = new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion
        };

        if (conexion is not null)
        {
            return await conexion.ExecuteScalarAsync<int>(sql, parametros, transaction);
        }

        using var propia = NuevaConexion();
        return await propia.ExecuteScalarAsync<int>(sql, parametros, transaction);
    }

    public async Task<bool> ActualizarClienteAsync(Cliente cliente, int id, IDbConnection? conexion = null, IDbTransaction? transaction = null)
    {
        var sql = @"UPDATE CLIENTE SET Nombre = @Nombre,Email = @Email,Telefono = @Telefono,Direccion = @Direccion WHERE id = @Id;";

        var parametros = new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion,
            Id = id
        };

        if (conexion is not null)
        {
            var rowsAffected = await conexion.ExecuteAsync(sql, parametros, transaction);
            return rowsAffected > 0;
        }

        using var propia = NuevaConexion();
        var rowsPropios = await propia.ExecuteAsync(sql, parametros, transaction);
        return rowsPropios > 0;
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int id, IDbConnection? conexion = null, IDbTransaction? transaction = null)
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE id = @Id;";

        if (conexion is not null)
        {
            return await conexion.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = id }, transaction);
        }

        using var propia = NuevaConexion();
        return await propia.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = id }, transaction);
    }

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string email, IDbConnection? conexion = null, IDbTransaction? transaction = null)
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE email = @Email;";

        if (conexion is not null)
        {
            return await conexion.QueryFirstOrDefaultAsync<Cliente>(sql, new { Email = email }, transaction);
        }

        using var propia = NuevaConexion();
        return await propia.QueryFirstOrDefaultAsync<Cliente>(sql, new { Email = email }, transaction);
    }

    public async Task<bool> ExisteEmailDeClienteAsync(string emailExistente)
    {
        var sql = "SELECT COUNT(1) FROM CLIENTE WHERE email = @Email";

        using var conexion = NuevaConexion();

        var count = await conexion.ExecuteScalarAsync<int>(sql, new { Email = emailExistente });
        return count > 0;
    }
}
