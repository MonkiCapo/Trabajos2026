using System;
using System.IO;
using MySqlConnector;

namespace Api.Pizzeria.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        var csBuilder = new MySqlConnectionStringBuilder(connectionString);
        string dbName = csBuilder.Database;

        // 1. Conectar SIN base de datos para verificar/crearla
        var serverCs = new MySqlConnectionStringBuilder(connectionString) { Database = "" };

        using (var conn = new MySqlConnection(serverCs.ConnectionString))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();

            // Verificar si la base de datos existe
            cmd.CommandText = $"SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = '{dbName}'";
            long exists = (long)cmd.ExecuteScalar()!;

            if (exists == 0)
            {
                Console.WriteLine($"[DB INFO] Database '{dbName}' not found. Creating...");
                cmd.CommandText = $"CREATE DATABASE `{dbName}`";
                cmd.ExecuteNonQuery();
                Console.WriteLine($"[DB INFO] Database '{dbName}' created.");
            }
            else
            {
                Console.WriteLine($"[DB INFO] Database '{dbName}' already exists.");
            }
        }

        // 2. Conectar a la base de datos
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        // 3. Ejecutar script (CREATE TABLE IF NOT EXISTS + INSERT IGNORE = seguro de ejecutar siempre)
        Console.WriteLine("[DB INFO] Running script.sql...");
        RunFullScript(connection);

        Console.WriteLine("[DB INFO] Database initialized successfully.");
    }

    private static void RunFullScript(MySqlConnection connection)
    {
        string scriptPath = FindScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
        {
            Console.WriteLine("[DB WARNING] script.sql was not found.");
            return;
        }

        Console.WriteLine($"[DB INFO] Using script: {scriptPath}");
        string sql = File.ReadAllText(scriptPath);

        // Eliminar líneas DROP/CREATE/USE DATABASE (ya se manejaron antes)
        var lines = sql.Split('\n');
        var filtered = lines.Where(l =>
        {
            string trimmed = l.TrimStart();
            return !trimmed.StartsWith("DROP DATABASE", StringComparison.OrdinalIgnoreCase) &&
                   !trimmed.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) &&
                   !trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase);
        });
        string cleanSql = string.Join('\n', filtered);

        using var command = new MySqlCommand(cleanSql, connection);
        command.ExecuteNonQuery();
        Console.WriteLine("[DB INFO] Script executed successfully.");
    }

    private static string FindScriptPath()
    {
        string[] searchPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "script.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "script.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "script.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "script.sql"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "script.sql"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "script.sql")
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return string.Empty;
    }
}
