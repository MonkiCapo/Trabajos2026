using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class ClienteRepositorio : DapperRepo, IClienteRepositorio
{
    public ClienteRepositorio(IAdo _ado) : base(_ado) { }

    public Cliente AgregarCliente(Cliente cliente)
    {
        var sql = @"INSERT INTO CLIENTE (nombre, email, telefono, direccion) 
                    VALUES (@Nombre, @Email, @Telefono, @Direccion);
                    SELECT LAST_INSERT_ID();";

        var id = Conexion.ExecuteScalar<int>(sql, new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion
        });
        cliente.Id = id;
        return cliente;
    }

    public bool ActualizarCliente(Cliente cliente, int id)
    {
        var sql = @"UPDATE CLIENTE
                    SET Nombre = @Nombre,
                        Email = @Email,
                        Telefono = @Telefono,
                        Direccion = @Direccion
                    WHERE id = @Id;";
        var rowsAffected = Conexion.Execute(sql, new
        {
            cliente.Nombre,
            cliente.Email,
            cliente.Telefono,
            cliente.Direccion,
            Id = id
        });
        return rowsAffected > 0;
    }

    public bool EliminarCliente(int id)
    {
        var sql = "DELETE FROM CLIENTE WHERE id = @Id;";
        var rowsAffected = Conexion.Execute(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public IEnumerable<Cliente> ObtenerClientes()
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE;";
        return Conexion.Query<Cliente>(sql);
    }

    public Cliente ObtenerClientePorId(int id)
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE id = @Id;";
        return Conexion.QueryFirstOrDefault<Cliente>(sql, new { Id = id });
    }

    public Cliente ObtenerClientePorEmail(string email)
    {
        var sql = "SELECT id, nombre, email, telefono, direccion FROM CLIENTE WHERE email = @Email;";
        return Conexion.QueryFirstOrDefault<Cliente>(sql, new { Email = email });
    }

    public bool ExisteEmailDeCliente(string emailExistente)
    {
        var sql = "SELECT COUNT(1) FROM CLIENTE WHERE email = @Email";
        var count = Conexion.ExecuteScalar<int>(sql, new { Email = emailExistente });
        return count > 0;
    }
}
