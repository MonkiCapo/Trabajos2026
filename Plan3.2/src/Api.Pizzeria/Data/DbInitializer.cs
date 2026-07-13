using System;
using System.IO;
using MySqlConnector;
using Dapper;

namespace Api.Pizzeria.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        string scriptPath = FindScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
        {
            Console.WriteLine("[DB WARNING] script.sql was not found. Database might be empty.");
            return;
        }

        Console.WriteLine($"[DB INFO] Initializing database using script at: {scriptPath}");
        string sql = File.ReadAllText(scriptPath);

        using var command = new MySqlCommand(sql, connection);
        command.ExecuteNonQuery();
        Console.WriteLine("[DB INFO] Database initialized successfully.");
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
