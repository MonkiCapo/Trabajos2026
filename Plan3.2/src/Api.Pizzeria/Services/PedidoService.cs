using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Net.Sockets;
using Api.Pizzeria.Models;
using Api.Pizzeria.Sockets;

namespace Api.Pizzeria.Services;

public class CocinaNoDisponibleException : Exception
{
    public int PedidoId { get; }
    public CocinaNoDisponibleException(string message, int pedidoId) : base(message)
    {
        PedidoId = pedidoId;
    }
}

public class PedidoService : IPedidoService
{
    private readonly string _connectionString;
    private readonly ISocketServer _socketServer;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(IConfiguration configuration, ISocketServer socketServer, ILogger<PedidoService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Port=3306;Database=5to_Pizzeria;User=root;Password=;";
        _socketServer = socketServer;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Verify client exists
            var clientExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM CLIENTE WHERE id = @Id", new { Id = nuevoPedido.ClienteId }, transaction);
            if (clientExists == 0)
            {
                throw new ArgumentException($"El cliente con ID {nuevoPedido.ClienteId} no existe.");
            }

            // 2. Fetch pizza prices by name and calculate total
            decimal calculatedTotal = 0;

            foreach (var item in nuevoPedido.Items)
            {
                var pizzaInfo = await connection.QuerySingleOrDefaultAsync<(int Id, string Nombre, decimal Precio)>(
                    "SELECT id, nombre, precio FROM PIZZA WHERE nombre = @Nombre",
                    new { Nombre = item.PizzaNombre }, transaction);

                if (pizzaInfo.Nombre == null)
                {
                    throw new ArgumentException($"La pizza \"{item.PizzaNombre}\" no existe en el catalogo.");
                }

                item.PizzaId = pizzaInfo.Id;
                item.PizzaNombre = pizzaInfo.Nombre;
                item.PrecioUnitario = pizzaInfo.Precio;
                calculatedTotal += pizzaInfo.Precio * item.Cantidad;
            }

            nuevoPedido.Total = calculatedTotal;
            nuevoPedido.Estado = EstadoPedido.EsperaConfirmacion;
            nuevoPedido.FechaCreacion = DateTime.UtcNow;
            nuevoPedido.FechaActualizacion = DateTime.UtcNow;

            // 3. Insert PEDIDO
            const string insertPedidoSql = @"
                INSERT INTO PEDIDO (cliente_id, estado_id, fecha_creacion, fecha_actualizacion, total)
                VALUES (@ClienteId, @EstadoId, @FechaCreacion, @FechaActualizacion, @Total);
                SELECT LAST_INSERT_ID();";

            var id = await connection.ExecuteScalarAsync<int>(insertPedidoSql, new
            {
                ClienteId = nuevoPedido.ClienteId,
                EstadoId = (int)nuevoPedido.Estado,
                FechaCreacion = nuevoPedido.FechaCreacion.ToString("o"),
                FechaActualizacion = nuevoPedido.FechaActualizacion.ToString("o"),
                Total = nuevoPedido.Total
            }, transaction);

            nuevoPedido.Id = id;

            // 4. Insert ITEMS
            const string insertItemSql = @"
                INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, cantidad, precio_unitario)
                VALUES (@PedidoId, @PizzaId, @Cantidad, @PrecioUnitario);";

            foreach (var item in nuevoPedido.Items)
            {
                item.PedidoId = nuevoPedido.Id;
                await connection.ExecuteAsync(insertItemSql, new
                {
                    PedidoId = item.PedidoId,
                    PizzaId = item.PizzaId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                }, transaction);
            }

            // 5. Insert HISTORIAL
            const string insertHistorialSql = @"
                INSERT INTO HISTORIAL_ESTADO_PEDIDO (pedido_id, estado_id, fecha_cambio, observacion)
                VALUES (@PedidoId, @EstadoId, @FechaCambio, @Observacion);";

            await connection.ExecuteAsync(insertHistorialSql, new
            {
                PedidoId = nuevoPedido.Id,
                EstadoId = (int)nuevoPedido.Estado,
                FechaCambio = DateTime.UtcNow.ToString("o"),
                Observacion = "Creación de pedido. Esperando confirmación de cocina."
            }, transaction);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "[PEDIDOSERVICE] Error creating order in DB.");
            throw;
        }

        // 6. Socket interaction with Cocina (outside transaction)
        _logger.LogInformation("[PEDIDOSERVICE] Attempting socket delivery to Cocina for pedido {Id}...", nuevoPedido.Id);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool ack = await _socketServer.EnviarPedidoACocinaAsync(nuevoPedido, cts.Token);

            if (ack)
            {
                _logger.LogInformation("[PEDIDOSERVICE] Cocina acknowledged pedido {Id}. Changing state to EnPreparacion.", nuevoPedido.Id);
                await ActualizarEstadoAsync(nuevoPedido.Id, EstadoPedido.EnPreparacion, "Cocina confirmó recepción (ACK)");
                nuevoPedido.Estado = EstadoPedido.EnPreparacion;
                nuevoPedido.FechaActualizacion = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning("[PEDIDOSERVICE] Cocina ACK failed (timeout or error) for pedido {Id}. Cancelling order.", nuevoPedido.Id);
                await ActualizarEstadoAsync(nuevoPedido.Id, EstadoPedido.Cancelado, "Fallo de comunicación con cocina (ACK no recibido)");
                throw new CocinaNoDisponibleException("La cocina no confirmó la recepción del pedido a tiempo.", nuevoPedido.Id);
            }
        }
        catch (Exception ex) when (ex is SocketException || ex is CocinaNoDisponibleException || ex is OperationCanceledException)
        {
            _logger.LogError(ex, "[PEDIDOSERVICE] Cocina is not available. Connection failed for pedido {Id}. Cancelling order.", nuevoPedido.Id);
            await ActualizarEstadoAsync(nuevoPedido.Id, EstadoPedido.Cancelado, $"Fallo de red: {ex.Message}");
            throw new CocinaNoDisponibleException("Servicio de cocina no disponible en este momento.", nuevoPedido.Id);
        }

        return nuevoPedido;
    }

    public async Task ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, string observacion)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            _logger.LogInformation("[PEDIDOSERVICE] Transitioning pedido {Id} to {Estado} - {Obs}", pedidoId, nuevoEstado, observacion);

            // Update pedido
            const string updateSql = @"
                UPDATE PEDIDO 
                SET estado_id = @EstadoId, fecha_actualizacion = @FechaActualizacion 
                WHERE id = @Id";

            await connection.ExecuteAsync(updateSql, new
            {
                EstadoId = (int)nuevoEstado,
                FechaActualizacion = DateTime.UtcNow.ToString("o"),
                Id = pedidoId
            }, transaction);

            // Insert historial
            const string insertHistorialSql = @"
                INSERT INTO HISTORIAL_ESTADO_PEDIDO (pedido_id, estado_id, fecha_cambio, observacion)
                VALUES (@PedidoId, @EstadoId, @FechaCambio, @Observacion);";

            await connection.ExecuteAsync(insertHistorialSql, new
            {
                PedidoId = pedidoId,
                EstadoId = (int)nuevoEstado,
                FechaCambio = DateTime.UtcNow.ToString("o"),
                Observacion = observacion
            }, transaction);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "[PEDIDOSERVICE] Error updating order status in DB for pedido {Id}.", pedidoId);
            throw;
        }

        // If the new state is EnViaje, trigger shipment to Reparto
        if (nuevoEstado == EstadoPedido.EnViaje)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var pedido = await GetPedidoByIdAsync(pedidoId);
                    if (pedido != null)
                    {
                        using var conn = CreateConnection();
                        var address = await conn.QuerySingleOrDefaultAsync<string>(
                            "SELECT direccion FROM CLIENTE WHERE id = @Id", new { Id = pedido.ClienteId });
                        
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        bool assigned = await _socketServer.EnviarPedidoARepartoAsync(pedido, address ?? "Sin dirección", cts.Token);
                        if (!assigned)
                        {
                            _logger.LogWarning("[PEDIDOSERVICE] Reparto was not available or timed out for assignment of pedido {Id}.", pedidoId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PEDIDOSERVICE] Failed to send assignment to Reparto for pedido {Id}.", pedidoId);
                }
            });
        }
    }

    public async Task<Pedido?> GetPedidoByIdAsync(int id)
    {
        using var connection = CreateConnection();
        connection.Open();

        const string sqlPedido = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion, fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO
            WHERE id = @Id";

        var pedido = await connection.QuerySingleOrDefaultAsync<Pedido>(sqlPedido, new { Id = id });
        if (pedido == null) return null;

        const string sqlItems = @"
            SELECT ip.id, ip.pedido_id AS PedidoId, ip.pizza_id AS PizzaId, ip.cantidad, ip.precio_unitario AS PrecioUnitario,
                   pz.nombre AS PizzaNombre
            FROM ITEM_PEDIDO ip
            JOIN PIZZA pz ON ip.pizza_id = pz.id
            WHERE ip.pedido_id = @PedidoId";

        var items = await connection.QueryAsync<ItemPedido>(sqlItems, new { PedidoId = id });
        pedido.Items = items.ToList();

        return pedido;
    }
}
