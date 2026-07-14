using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;
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
    private readonly IClienteRepositorio _clienteRepo;
    private readonly IPedidoRepositorio _pedidoRepo;
    private readonly IPizzaRepositorio _pizzaRepo;
    private readonly IAdo _ado;
    private readonly ISocketServer _socketServer;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        IClienteRepositorio clienteRepo,
        IPedidoRepositorio pedidoRepo,
        IPizzaRepositorio pizzaRepo,
        IAdo ado,
        ISocketServer socketServer,
        ILogger<PedidoService> logger)
    {
        _clienteRepo = clienteRepo;
        _pedidoRepo = pedidoRepo;
        _pizzaRepo = pizzaRepo;
        _ado = ado;
        _socketServer = socketServer;
        _logger = logger;
    }

    public async Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido)
    {
        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        try
        {
            // 1. Verificar que el cliente existe
            var cliente = await _clienteRepo.ObtenerClientePorIdAsync(nuevoPedido.ClienteId);
            if (cliente == null)
            {
                throw new ArgumentException($"El cliente con ID {nuevoPedido.ClienteId} no existe.");
            }

            // 2. Obtener precios de pizzas por nombre y calcular total
            decimal calculatedTotal = 0;

            foreach (var item in nuevoPedido.Items)
            {
                var pizzaInfo = await _pizzaRepo.ObtenerPizzaPorNombreAsync(item.PizzaNombre);

                if (pizzaInfo == null)
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

            // 3. Insertar PEDIDO
            var pedidoId = await _pedidoRepo.CrearPedidoAsync(nuevoPedido, conexion, transaction);
            nuevoPedido.Id = pedidoId;

            // 4. Insertar ITEMS
            await _pedidoRepo.CrearItemsPedidoAsync(nuevoPedido.Items, pedidoId, conexion, transaction);

            // 5. Insertar HISTORIAL
            await _pedidoRepo.CrearHistorialAsync(pedidoId, EstadoPedido.EsperaConfirmacion, 
                "Creación de pedido. Esperando confirmación de cocina.", conexion, transaction);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "[PEDIDOSERVICE] Error al crear pedido en la base de datos.");
            throw;
        }

        // 6. Interacción socket con Cocina (fuera de la transacción)
        _logger.LogInformation("[PEDIDOSERVICE] Intentando enviar pedido por socket a Cocina para pedido {Id}...", nuevoPedido.Id);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            bool ack = await _socketServer.EnviarPedidoACocinaAsync(nuevoPedido, cts.Token);

            if (ack)
            {
                _logger.LogInformation("[PEDIDOSERVICE] Cocina confirmó recepción del pedido {Id}. Cambiando estado a EnPreparacion.", nuevoPedido.Id);
                await ActualizarEstadoAsync(nuevoPedido.Id, EstadoPedido.EnPreparacion, "Cocina confirmó recepción (ACK)");
                nuevoPedido.Estado = EstadoPedido.EnPreparacion;
                nuevoPedido.FechaActualizacion = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning("[PEDIDOSERVICE] ACK de Cocina fallido (timeout o error) para pedido {Id}. Cancelando pedido.", nuevoPedido.Id);
                await ActualizarEstadoAsync(nuevoPedido.Id, EstadoPedido.Cancelado, "Fallo de comunicación con cocina (ACK no recibido)");
                throw new CocinaNoDisponibleException("La cocina no confirmó la recepción del pedido a tiempo.", nuevoPedido.Id);
            }
        }
        catch (Exception ex) when (ex is SocketException || ex is CocinaNoDisponibleException || ex is OperationCanceledException)
        {
            _logger.LogError(ex, "[PEDIDOSERVICE] Cocina no disponible. Falló la conexión para pedido {Id}. Cancelando pedido.", nuevoPedido.Id);
            await ActualizarEstadoAsync(nuevoPedido.Id, EstadoPedido.Cancelado, $"Fallo de red: {ex.Message}");
            throw new CocinaNoDisponibleException("Servicio de cocina no disponible en este momento.", nuevoPedido.Id);
        }

        return nuevoPedido;
    }

    public async Task ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, string observacion)
    {
        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        try
        {
            _logger.LogInformation("[PEDIDOSERVICE] Transicionando pedido {Id} a {Estado} - {Obs}", pedidoId, nuevoEstado, observacion);

            // Actualizar pedido
            await _pedidoRepo.ActualizarEstadoAsync(pedidoId, nuevoEstado, conexion, transaction);

            // Insertar historial
            await _pedidoRepo.CrearHistorialAsync(pedidoId, nuevoEstado, observacion, conexion, transaction);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "[PEDIDOSERVICE] Error al actualizar estado del pedido en la base de datos para pedido {Id}.", pedidoId);
            throw;
        }

        // Si el nuevo estado es EnViaje, activar envío a Reparto
        if (nuevoEstado == EstadoPedido.EnViaje)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var pedido = await _pedidoRepo.ObtenerPedidoPorIdAsync(pedidoId);
                    if (pedido != null)
                    {
                        var cliente = await _clienteRepo.ObtenerClientePorIdAsync(pedido.ClienteId);
                        
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        bool assigned = await _socketServer.EnviarPedidoARepartoAsync(pedido, cliente?.Direccion ?? "Sin dirección", cts.Token);
                        if (!assigned)
                        {
                            _logger.LogWarning("[PEDIDOSERVICE] Reparto no disponible o timeout para la asignación del pedido {Id}.", pedidoId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PEDIDOSERVICE] Falló el envío de asignación a Reparto para pedido {Id}.", pedidoId);
                }
            });
        }
    }

    public async Task<Pedido?> GetPedidoByIdAsync(int id)
    {
        return await _pedidoRepo.ObtenerPedidoPorIdAsync(id);
    }
}
