using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class ClienteRepositorio : DapperRepo, IClienteRepositorio
{
    private const string Columnas = "id, nombre, email, telefono, direccion";

    public ClienteRepositorio(IAdo ado) : base(ado) { }

    public async Task<IEnumerable<Cliente>> ObtenerClientesAsync(CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM CLIENTE;";
        return await conexion.QueryAsync<Cliente>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int id, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM CLIENTE WHERE id = @Id;";
        return await conexion.QueryFirstOrDefaultAsync<Cliente>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string email, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = $"SELECT {Columnas} FROM CLIENTE WHERE email = @Email;";
        return await conexion.QueryFirstOrDefaultAsync<Cliente>(new CommandDefinition(sql, new { Email = email }, cancellationToken: ct));
    }

    public async Task<int> AgregarClienteAsync(Cliente cliente, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = @"INSERT INTO CLIENTE (nombre, email, telefono, direccion)
                    VALUES (@Nombre, @Email, @Telefono, @Direccion);
                    SELECT LAST_INSERT_ID();";
        return await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion
        }, cancellationToken: ct));
    }

    public async Task<bool> ActualizarClienteAsync(Cliente cliente, int id, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = @"UPDATE CLIENTE
                    SET nombre = @Nombre, email = @Email, telefono = @Telefono, direccion = @Direccion
                    WHERE id = @Id;";
        var rowsAffected = await conexion.ExecuteAsync(new CommandDefinition(sql, new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion,
            Id = id
        }, cancellationToken: ct));
        return rowsAffected > 0;
    }

    public async Task<bool> EliminarClienteAsync(int id, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = "DELETE FROM CLIENTE WHERE id = @Id;";
        var rowsAffected = await conexion.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return rowsAffected > 0;
    }

    public async Task<bool> ExisteEmailDeClienteAsync(string emailExistente, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        var sql = "SELECT COUNT(1) FROM CLIENTE WHERE email = @Email;";
        var count = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Email = emailExistente }, cancellationToken: ct));
        return count > 0;
    }
}