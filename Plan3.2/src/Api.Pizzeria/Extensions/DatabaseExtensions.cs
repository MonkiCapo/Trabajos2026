using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Api.Pizzeria.Data;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.IRepositorios;
using Dapper.Pizzeria;

namespace Api.Pizzeria.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddPizzeriaDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Aca busco la conexión una de las dos cadenas de conexión que tengo (Una de casa y otra del colegio)
        string connectionString = ObtenerCadenaConexionActiva(configuration);

        // Registro la conexión a la base de datos
        services.AddSingleton<IAdo>(new Ado(connectionString));

        // Registro repositorios del Dapper.Pizzeria
        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
        services.AddScoped<IPizzaRepositorio, PizzaRepositorio>();

        // Se crea la bd buscando el archivo script.sql
        DbInitializer.Initialize(connectionString);

        return services;
    }

    public static string ObtenerCadenaConexionActiva(IConfiguration configuration)
    {
        var candidates = new[]
        {
            configuration.GetConnectionString("DefaultConnection"),
            configuration.GetConnectionString("CasaConnection"),
            configuration.GetConnectionString("ColegioConnection"),
            "Server=localhost;Port=3306;Database=5to_Pizzeria;User=root;Password=root;",
            "Server=localhost;Port=3306;Database=5to_Pizzeria;User=5to_agbd;Password=Trigg3rs!;"
        }.Where(cs => !string.IsNullOrWhiteSpace(cs)).Distinct();

        foreach (var cs in candidates)
        {
            try
            {
                var serverCs = new MySqlConnectionStringBuilder(cs!) { Database = "" };
                using var conn = new MySqlConnection(serverCs.ConnectionString);
                conn.Open();
                Console.WriteLine($"[DB INFO] Conexión MySQL establecida con usuario '{serverCs.UserID}'.");
                return cs!;
            }
            catch
            {
                // Probar con la siguiente cadena
            }
        }

        throw new InvalidOperationException("No se pudo conectar a MySQL con ninguna de las cadenas de conexión configuradas (Casa / Colegio).");
    }
}
