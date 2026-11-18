using System.Data.Common;
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

    public DbConnection GetDbConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}