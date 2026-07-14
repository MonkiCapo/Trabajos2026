using System.Data;
using MySqlConnector;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class Ado : IAdo
{
    private readonly string _connectionString;

    public Ado(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection GetDbConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}
