using System;
using System.IO;
using System.Text.RegularExpressions;
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
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = @dbName";
            cmd.Parameters.AddWithValue("@dbName", dbName);
            long exists = (long)cmd.ExecuteScalar()!;

            if (exists == 0)
            {
                if (!Regex.IsMatch(dbName, "^[A-Za-z0-9_]+$"))
                {
                    throw new InvalidOperationException($"Nombre de base de datos inválido: '{dbName}'.");
                }

                Console.WriteLine($"[DB INFO] Database '{dbName}' not found. Creating...");
                cmd.Parameters.Clear();
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

        // 3. Migración idempotente: agregar columnas nuevas a bases ya existentes.
        //    En bases nuevas el script las crea; acá solo se aplican si la tabla existe y faltan.
        EnsureColumn(connection, "PIZZA", "descripcion", "TEXT");
        EnsureColumn(connection, "PIZZA", "stock", "INT NOT NULL DEFAULT 0");
        EnsureColumn(connection, "PIZZA", "disponible", "TINYINT(1) NOT NULL DEFAULT 1");

        // 4. Ejecutar script (CREATE TABLE IF NOT EXISTS + seeds idempotentes = seguro de ejecutar siempre)
        Console.WriteLine("[DB INFO] Running script.sql...");
        RunFullScript(connection);

        Console.WriteLine("[DB INFO] Database initialized successfully.");
    }

    private static void EnsureColumn(MySqlConnection connection, string table, string column, string definition)
    {
        using var tableCmd = connection.CreateCommand();
        tableCmd.CommandText = @"SELECT COUNT(*) FROM information_schema.TABLES
                                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @Table";
        tableCmd.Parameters.AddWithValue("@Table", table);
        if ((long)tableCmd.ExecuteScalar()! == 0)
        {
            return;
        }

        using var colCmd = connection.CreateCommand();
        colCmd.CommandText = @"SELECT COUNT(*) FROM information_schema.COLUMNS
                               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @Table AND COLUMN_NAME = @Column";
        colCmd.Parameters.AddWithValue("@Table", table);
        colCmd.Parameters.AddWithValue("@Column", column);
        if ((long)colCmd.ExecuteScalar()! > 0)
        {
            return;
        }

        using var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}";
        alterCmd.ExecuteNonQuery();
        Console.WriteLine($"[DB INFO] Column '{table}.{column}' added.");
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
