using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class ClienteRepositorio : DapperRepo, IClienteRepositorio
{
    public ClienteRepositorio(IAdo _ado) : base(_ado) { }

    public async Task<int> AgregarClienteAsync(Cliente cliente)
    {
        var sql = @"INSERT INTO CLIENTE (nombre, email, telefono, direccion) 
                    VALUES (@Nombre, @Email, @Telefono, @Direccion);
                    SELECT LAST_INSERT_ID();";

        var id = await Conexion.ExecuteScalarAsync<int>(sql, new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion
        });
        return id;
    }

    public async Task<bool> ActualizarClienteAsync(Cliente cliente, int id)
    {
        var sql = @"UPDATE CLIENTE
                    SET Nombre = @Nombre,
                        Email = @Email,
                        Telefono = @Telefono,
                        Direccion = @Direccion
                    WHERE id = @Id;";
        var rowsAffected = await Conexion.ExecuteAsync(sql, new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion,
            Id = id
        });
        return rowsAffected > 0;
    }

    public async Task<bool> EliminarClienteAsync(int id)
    {
        var sql = "DELETE FROM CLIENTE WHERE id = @Id;";
        var rowsAffected = await Conexion.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<Cliente>> ObtenerClientesAsync()
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE;";
        return await Conexion.QueryAsync<Cliente>(sql);
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int id)
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE id = @Id;";
        return await Conexion.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = id });
    }

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string email)
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE email = @Email;";
        return await Conexion.QueryFirstOrDefaultAsync<Cliente>(sql, new { Email = email });
    }

    public async Task<bool> ExisteEmailDeClienteAsync(string emailExistente)
    {
        var sql = "SELECT COUNT(1) FROM CLIENTE WHERE email = @Email";
        var count = await Conexion.ExecuteScalarAsync<int>(sql, new { Email = emailExistente });
        return count > 0;
    }
}
