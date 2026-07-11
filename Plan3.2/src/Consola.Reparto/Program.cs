using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Consola.Reparto;

class Program
{
    private const string Host = "127.0.0.1";
    private const int Port = 7000;

    static async Task Main(string[] args)
    {
        Console.Title = "Pizzería - Reparto";
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("==================================================");
        Console.WriteLine("        MODULO DE REPARTO - PIZZERÍA");
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
                var identification = new { accion = "identificar", tipo = "reparto" };
                string idJson = JsonSerializer.Serialize(identification) + "\n";
                await writer.WriteAsync(idJson);

                // 2. Escuchar entregas asignadas
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

            if (accion == "asignar_entrega")
            {
                int pedidoId = root.GetProperty("pedidoId").GetInt32();
                string direccion = root.GetProperty("direccion").GetString() ?? "Sin Dirección";

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] NUEVA ENTREGA ASIGNADA.");
                Console.WriteLine($"Pedido: #{pedidoId}");
                Console.WriteLine($"Dirección de envío: {direccion}");
                Console.ResetColor();

                // Enviar ACK inmediato
                var ack = new { accion = "ack", pedidoId = pedidoId, status = "recibido" };
                string ackJson = JsonSerializer.Serialize(ack) + "\n";
                await writer.WriteAsync(ackJson);
                Console.WriteLine($"[ACK] Asignación recibida para Pedido #{pedidoId}.");

                // Simular reparto en hilo secundario
                _ = Task.Run(async () =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[LOGISTICA] Repartidor en viaje hacia {direccion} (simulando 6 segundos)...");
                    Console.ResetColor();
                    
                    await Task.Delay(6000); // 6 segundos de viaje
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[LOGISTICA] Pedido #{pedidoId} entregado exitosamente.");
                    Console.ResetColor();

                    // Notificar entrega al backend
                    var deliveredMsg = new { accion = "pedido_entregado", pedidoId = pedidoId };
                    string deliveredJson = JsonSerializer.Serialize(deliveredMsg) + "\n";
                    
                    lock (writer)
                    {
                        try
                        {
                            writer.Write(deliveredJson);
                            writer.Flush();
                            Console.WriteLine($"[NOTIFICACION] Evento 'pedido_entregado' enviado para Pedido #{pedidoId}.");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error al enviar pedido_entregado #{pedidoId}: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error procesando asignación: {ex.Message}");
            Console.ResetColor();
        }
    }
}
