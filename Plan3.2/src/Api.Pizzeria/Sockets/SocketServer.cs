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
    
    // Connected clients
    private TcpClient? _cocinaClient;
    private TcpClient? _repartoClient;
    private readonly object _lock = new();

    // ACK collections mapping pedidoId -> TaskCompletionSource
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
        _logger.LogInformation("[SOCKETSERVER] Server started on port {Port}.", Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _logger.LogInformation("[SOCKETSERVER] New TCP connection accepted.");
                _ = HandleConnectionAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[SOCKETSERVER] Stopping listener.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Exception in socket acceptance loop.");
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
            // The first message must identify the client type
            string? line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) return;

            var handshake = JsonSerializer.Deserialize<SocketMessage>(line, _jsonOptions);
            if (handshake == null || handshake.Accion != "identificar")
            {
                _logger.LogWarning("[SOCKETSERVER] Handshake failed. Expected identification message. Received: {Line}", line);
                client.Close();
                return;
            }

            clientType = handshake.Tipo?.ToLowerInvariant();
            lock (_lock)
            {
                if (clientType == "cocina")
                {
                    _cocinaClient?.Close(); // Disconnect existing
                    _cocinaClient = client;
                    _logger.LogInformation("[SOCKETSERVER] Cocina connected and identified.");
                }
                else if (clientType == "reparto")
                {
                    _repartoClient?.Close(); // Disconnect existing
                    _repartoClient = client;
                    _logger.LogInformation("[SOCKETSERVER] Reparto connected and identified.");
                }
                else
                {
                    _logger.LogWarning("[SOCKETSERVER] Unknown client type: {Type}", handshake.Tipo);
                    client.Close();
                    return;
                }
            }

            // Continuous read loop
            while (!ct.IsCancellationRequested)
            {
                line = await reader.ReadLineAsync(ct);
                if (line == null)
                {
                    _logger.LogInformation("[SOCKETSERVER] Client {Type} disconnected gracefully.", clientType);
                    break; // Client closed connection
                }

                _logger.LogInformation("[SOCKETSERVER] Received from {Type}: {Msg}", clientType, line);
                _ = ProcessClientMessageAsync(line, clientType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Exception handling {Type} connection.", clientType ?? "unidentified");
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
                _logger.LogInformation("[SOCKETSERVER] Cocina finished pedido {PedidoId}. Transitioning state.", msg.PedidoId);
                
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPedidoService>();
                await service.ActualizarEstadoAsync(msg.PedidoId, EstadoPedido.EnViaje, "Cocina finalizó la preparación");
            }
            else if (msg.Accion == "pedido_entregado")
            {
                _logger.LogInformation("[SOCKETSERVER] Reparto delivered pedido {PedidoId}. Transitioning state.", msg.PedidoId);
                
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IPedidoService>();
                await service.ActualizarEstadoAsync(msg.PedidoId, EstadoPedido.Entregado, "Reparto completó la entrega");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Error processing client message from {Type}: {Line}", clientType, line);
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
            _logger.LogWarning("[SOCKETSERVER] Cannot send order: Cocina is not connected.");
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
            _logger.LogInformation("[SOCKETSERVER] Sending pedido {Id} to Cocina...", pedido.Id);
            await writer.WriteAsync(json);

            // Wait for ACK
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000, ct));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                _logger.LogWarning("[SOCKETSERVER] Timeout waiting for ACK from Cocina for pedido {Id}.", pedido.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Exception sending pedido {Id} to Cocina.", pedido.Id);
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
            _logger.LogWarning("[SOCKETSERVER] Cannot assign order: Reparto is not connected.");
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
            _logger.LogInformation("[SOCKETSERVER] Assigning delivery of pedido {Id} to Reparto...", pedido.Id);
            await writer.WriteAsync(json);

            // Wait for ACK
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000, ct));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                _logger.LogWarning("[SOCKETSERVER] Timeout waiting for ACK from Reparto for pedido {Id}.", pedido.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SOCKETSERVER] Exception sending assignment of pedido {Id} to Reparto.", pedido.Id);
            return false;
        }
        finally
        {
            _repartoAcks.TryRemove(pedido.Id, out _);
        }
    }
}

// Support classes for Socket Communication
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
