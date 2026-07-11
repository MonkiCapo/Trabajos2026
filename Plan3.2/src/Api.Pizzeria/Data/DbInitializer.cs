using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Api.Pizzeria.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        // SQLite will automatically create the file if it doesn't exist.
        // Let's execute the SQL script to create tables and seed data.
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Find the script.sql file
        string scriptPath = FindScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
        {
            Console.WriteLine("[DB WARNING] script.sql was not found. Database might be empty.");
            return;
        }

        Console.WriteLine($"[DB INFO] Initializing database using script at: {scriptPath}");
        string sql = File.ReadAllText(scriptPath);

        // SQLite Dapper call
        connection.Execute(sql);
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
