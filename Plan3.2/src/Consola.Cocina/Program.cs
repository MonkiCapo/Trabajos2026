using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Consola.Cocina;

class Program
{
    private const string Host = "127.0.0.1";
    private const int Port = 7000;

    static async Task Main(string[] args)
    {
        Console.Title = "Pizzería - Cocina";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("==================================================");
        Console.WriteLine("        MODULO DE COCINA - PIZZERÍA");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        while (true)
        {
            try
            {
                Console.WriteLine($"Conectando al servidor backend ({Host}:{Port})...");
                using var client = new TcpClient();
                await client.ConnectAsync(Host, Port);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("¡Conexión establecida con el Backend!");
                Console.ResetColor();

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                // 1. Enviar Identificación
                var identification = new { accion = "identificar", tipo = "cocina" };
                string idJson = JsonSerializer.Serialize(identification) + "\n";
                await writer.WriteAsync(idJson);

                // 2. Escuchar pedidos entrantes
                while (client.Connected)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("El servidor backend cerró la conexión.");
                        Console.ResetColor();
                        break;
                    }

                    _ = ProcessMessageAsync(line, writer);
                }
            }
            catch (SocketException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No se pudo conectar al Backend. Reintentando en 3 segundos...");
                Console.ResetColor();
                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error de red: {ex.Message}. Reconectando...");
                Console.ResetColor();
                await Task.Delay(3000);
            }
        }
    }

    private static async Task ProcessMessageAsync(string line, StreamWriter writer)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            string accion = root.GetProperty("accion").GetString() ?? "";

            if (accion == "nuevo_pedido")
            {
                int pedidoId = root.GetProperty("pedidoId").GetInt32();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] NUEVO PEDIDO #{pedidoId} RECIBIDO.");
                Console.ResetColor();

                if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine("Detalle del pedido:");
                    foreach (var item in itemsProp.EnumerateArray())
                    {
                        string nombre = item.GetProperty("pizzaNombre").GetString() ?? "Pizza";
                        int cantidad = item.GetProperty("cantidad").GetInt32();
                        Console.WriteLine($"  - {cantidad}x {nombre}");
                    }
                }

                // Enviar ACK inmediato
                var ack = new { accion = "ack", pedidoId = pedidoId, status = "recibido" };
                string ackJson = JsonSerializer.Serialize(ack) + "\n";
                await writer.WriteAsync(ackJson);
                Console.WriteLine($"[ACK] Confirmada recepción de pedido #{pedidoId} al backend.");

                // Simular cocción en hilo secundario para no bloquear el canal de lectura
                _ = Task.Run(async () =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[PROCESO] Cocinando pedido #{pedidoId} (simulando 6 segundos)...");
                    Console.ResetColor();
                    
                    await Task.Delay(6000); // 6 segundos de cocción
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[PROCESO] Pedido #{pedidoId} está listo.");
                    Console.ResetColor();

                    // Notificar al backend
                    var preparedMsg = new { accion = "pedido_preparado", pedidoId = pedidoId };
                    string preparedJson = JsonSerializer.Serialize(preparedMsg) + "\n";
                    
                    // Necesitamos sincronizar acceso al writer
                    lock (writer)
                    {
                        try
                        {
                            writer.Write(preparedJson);
                            writer.Flush();
                            Console.WriteLine($"[NOTIFICACION] Evento 'pedido_preparado' enviado para Pedido #{pedidoId}.");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error al enviar pedido_preparado #{pedidoId}: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error procesando mensaje: {ex.Message}");
            Console.ResetColor();
        }
    }
}
