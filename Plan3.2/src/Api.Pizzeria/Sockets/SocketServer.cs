using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Api.Pizzeria.Models;
using Api.Pizzeria.Services;

namespace Api.Pizzeria.Sockets;

public class SocketServer : BackgroundService, ISocketServer
{
    private readonly ILogger<SocketServer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private TcpListener? _listener;
    
    // Clientes conectados
    private TcpClient? _cocinaClient;
    private TcpClient? _repartoClient;
    private readonly object _lock = new();

    // Colecciones ACK mapeando pedidoId -> TaskCompletionSource
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _cocinaAcks = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _repartoAcks = new();

    private const int Port = 7000;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SocketServer(ILogger<SocketServer> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _logger.LogInformation("[SOCKETSERVER] Servidor iniciado en el puerto {Port}.", Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _logger.LogInformation("[SOCKETSERVER] Nueva conexión TCP aceptada.");
                _ = HandleConnectionAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[SOCKETSERVER] Deteniendo escuchador.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Excepción en el bucle de aceptación de sockets.");
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleConnectionAsync(TcpClient client, CancellationToken ct)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        
        string? clientType = null;

        try
        {
            // El primer mensaje debe identificar el tipo de cliente
            string? line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) return;

            var handshake = JsonSerializer.Deserialize<SocketMessage>(line, _jsonOptions);
            if (handshake == null || handshake.Accion != "identificar")
            {
                _logger.LogWarning("[SOCKETSERVER] Handshake fallido. Se esperaba mensaje de identificación. Recibido: {Line}", line);
                client.Close();
                return;
            }

            clientType = handshake.Tipo?.ToLowerInvariant();
            lock (_lock)
            {
                if (clientType == "cocina")
                {
                    _cocinaClient?.Close(); // Desconectar existente
                    _cocinaClient = client;
                    _logger.LogInformation("[SOCKETSERVER] Cocina conectada e identificada.");
                }
                else if (clientType == "reparto")
                {
                    _repartoClient?.Close(); // Desconectar existente
                    _repartoClient = client;
                    _logger.LogInformation("[SOCKETSERVER] Reparto conectado e identificado.");
                }
                else
                {
                    _logger.LogWarning("[SOCKETSERVER] Tipo de cliente desconocido: {Type}", handshake.Tipo);
                    client.Close();
                    return;
                }
            }

            // Bucle de lectura continua
            while (!ct.IsCancellationRequested)
            {
                line = await reader.ReadLineAsync(ct);
                if (line == null)
                {
                    _logger.LogInformation("[SOCKETSERVER] Cliente {Type} desconectado gracefulamente.", clientType);
                    break; // El cliente cerró la conexión
                }

                _logger.LogInformation("[SOCKETSERVER] Recibido de {Type}: {Msg}", clientType, line);
                _ = ProcessClientMessageAsync(line, clientType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Excepción manejando conexión de {Type}.", clientType ?? "no identificado");
        }
        finally
        {
            lock (_lock)
            {
                if (clientType == "cocina" && _cocinaClient == client)
                {
                    _cocinaClient = null;
                }
                else if (clientType == "reparto" && _repartoClient == client)
                {
                    _repartoClient = null;
                }
            }
            client.Close();
        }
    }

    private async Task ProcessClientMessageAsync(string line, string clientType)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<SocketMessage>(line, _jsonOptions);
            if (msg == null) return;

            if (msg.Accion == "ack")
            {
                if (clientType == "cocina")
                {
                    if (_cocinaAcks.TryGetValue(msg.PedidoId, out var tcs))
                    {
                        tcs.TrySetResult(true);
                    }
                }
                else if (clientType == "reparto")
                {
                    if (_repartoAcks.TryGetValue(msg.PedidoId, out var tcs))
                    {
                        tcs.TrySetResult(true);
                    }
                }
            }
            else if (msg.Accion == "pedido_preparado")
            {
                _logger.LogInformation("[SOCKETSERVER] Cocina finalizó pedido {PedidoId}. Transicionando estado.", msg.PedidoId);
                
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPedidoService>();
                await service.ActualizarEstadoAsync(msg.PedidoId, EstadoPedido.EnViaje, "Cocina finalizó la preparación");
            }
            else if (msg.Accion == "pedido_entregado")
            {
                _logger.LogInformation("[SOCKETSERVER] Reparto entregó pedido {PedidoId}. Transicionando estado.", msg.PedidoId);
                
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPedidoService>();
                await service.ActualizarEstadoAsync(msg.PedidoId, EstadoPedido.Entregado, "Reparto completó la entrega");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Error al procesar mensaje del cliente {Type}: {Line}", clientType, line);
        }
    }

    public async Task<bool> EnviarPedidoACocinaAsync(Pedido pedido, CancellationToken ct)
    {
        TcpClient? client;
        lock (_lock)
        {
            client = _cocinaClient;
        }

        if (client == null || !client.Connected)
        {
            _logger.LogWarning("[SOCKETSERVER] No se puede enviar pedido: Cocina no está conectada.");
            throw new SocketException((int)SocketError.ConnectionRefused);
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _cocinaAcks[pedido.Id] = tcs;

        try
        {
            var stream = client.GetStream();
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            var itemsList = new List<SocketItem>();
            foreach (var item in pedido.Items)
            {
                itemsList.Add(new SocketItem
                {
                    PizzaId = item.PizzaId,
                    PizzaNombre = item.PizzaNombre,
                    Cantidad = item.Cantidad
                });
            }

            var msg = new SocketMessage
            {
                Accion = "nuevo_pedido",
                PedidoId = pedido.Id,
                Items = itemsList
            };

            string json = JsonSerializer.Serialize(msg, _jsonOptions) + "\n";
            _logger.LogInformation("[SOCKETSERVER] Enviando pedido {Id} a Cocina...", pedido.Id);
            await writer.WriteAsync(json);

            // Wait for ACK
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000, ct));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                _logger.LogWarning("[SOCKETSERVER] Timeout esperando ACK de Cocina para pedido {Id}.", pedido.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Excepción al enviar pedido {Id} a Cocina.", pedido.Id);
            throw;
        }
        finally
        {
            _cocinaAcks.TryRemove(pedido.Id, out _);
        }
    }

    public async Task<bool> EnviarPedidoARepartoAsync(Pedido pedido, string direccion, CancellationToken ct)
    {
        TcpClient? client;
        lock (_lock)
        {
            client = _repartoClient;
        }

        if (client == null || !client.Connected)
        {
            _logger.LogWarning("[SOCKETSERVER] No se puede asignar pedido: Reparto no está conectado.");
            return false;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _repartoAcks[pedido.Id] = tcs;

        try
        {
            var stream = client.GetStream();
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            var msg = new SocketMessage
            {
                Accion = "asignar_entrega",
                PedidoId = pedido.Id,
                Direccion = direccion
            };

            string json = JsonSerializer.Serialize(msg, _jsonOptions) + "\n";
            _logger.LogInformation("[SOCKETSERVER] Asignando entrega del pedido {Id} a Reparto...", pedido.Id);
            await writer.WriteAsync(json);

            // Esperar ACK
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000, ct));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                _logger.LogWarning("[SOCKETSERVER] Timeout esperando ACK de Reparto para pedido {Id}.", pedido.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Excepción al enviar asignación del pedido {Id} a Reparto.", pedido.Id);
            return false;
        }
        finally
        {
            _repartoAcks.TryRemove(pedido.Id, out _);
        }
    }
}

// Clases de soporte para comunicación Socket
public class SocketMessage
{
    public string Accion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public int PedidoId { get; set; }
    public string? Status { get; set; }
    public string? Direccion { get; set; }
    public List<SocketItem>? Items { get; set; }
}

public class SocketItem
{
    public int PizzaId { get; set; }
    public string PizzaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}
