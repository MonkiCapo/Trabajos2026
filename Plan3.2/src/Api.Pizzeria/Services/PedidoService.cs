using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.Excepciones;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Api.Pizzeria.Services;

public class PedidoService : IPedidoService
{
    private static readonly Dictionary<EstadoPedido, EstadoPedido> TransicionesValidas = new()
    {
        [EstadoPedido.EsperaConfirmacion] = EstadoPedido.EnPreparacion,
        [EstadoPedido.EnPreparacion] = EstadoPedido.EnViaje,
        [EstadoPedido.EnViaje] = EstadoPedido.Entregado
    };

    private readonly IClienteRepositorio _clienteRepo;
    private readonly IPedidoRepositorio _pedidoRepo;
    private readonly IPizzaRepositorio _pizzaRepo;
    private readonly IAdo _ado;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        IClienteRepositorio clienteRepo,
        IPedidoRepositorio pedidoRepo,
        IPizzaRepositorio pizzaRepo,
        IAdo ado,
        ILogger<PedidoService> logger)
    {
        _clienteRepo = clienteRepo;
        _pedidoRepo = pedidoRepo;
        _pizzaRepo = pizzaRepo;
        _ado = ado;
        _logger = logger;
    }

    public async Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido, CancellationToken ct = default)
    {
        using var conexion = _ado.GetDbConnection();
        await conexion.OpenAsync(ct);
        using var transaction = await conexion.BeginTransactionAsync(ct);

        try
        {
            // 1. Verificar que el cliente existe
            var cliente = await _clienteRepo.ObtenerClientePorIdAsync(nuevoPedido.ClienteId, ct);
            if (cliente == null)
            {
                throw new ArgumentException($"El cliente con ID {nuevoPedido.ClienteId} no existe.");
            }

            // 2. Una sola consulta para todas las pizzas del pedido (evita N+1)
            var nombres = nuevoPedido.Items.Select(i => i.PizzaNombre).Distinct().ToList();
            var pizzas = (await _pizzaRepo.ObtenerPizzasPorNombresAsync(nombres, ct))
                .ToDictionary(p => p.Nombre, StringComparer.OrdinalIgnoreCase);

            decimal calculatedTotal = 0;

            foreach (var item in nuevoPedido.Items)
            {
                if (!pizzas.TryGetValue(item.PizzaNombre, out var pizzaInfo))
                {
                    throw new ArgumentException($"La pizza \"{item.PizzaNombre}\" no existe en el catalogo.");
                }

                if (!pizzaInfo.Disponible)
                {
                    throw new ArgumentException($"La pizza \"{item.PizzaNombre}\" no está disponible actualmente.");
                }

                item.PizzaId = pizzaInfo.Id;
                item.PizzaNombre = pizzaInfo.Nombre;
                item.PrecioUnitario = pizzaInfo.Precio;
                calculatedTotal += pizzaInfo.Precio * item.Cantidad;

                // Descuento atómico de stock dentro de la misma transacción.
                var descontado = await _pizzaRepo.DescontarStockAsync(pizzaInfo.Id, item.Cantidad, conexion, transaction, ct);
                if (!descontado)
                {
                    throw new ArgumentException($"Stock insuficiente para la pizza \"{item.PizzaNombre}\". Disponible: {pizzaInfo.Stock}, solicitado: {item.Cantidad}.");
                }
            }

            nuevoPedido.Total = calculatedTotal;
            nuevoPedido.Estado = EstadoPedido.EsperaConfirmacion;
            nuevoPedido.FechaCreacion = DateTime.UtcNow;
            nuevoPedido.FechaActualizacion = DateTime.UtcNow;

            // 3. Insertar PEDIDO
            var pedidoId = await _pedidoRepo.CrearPedidoAsync(nuevoPedido, conexion, transaction, ct);
            nuevoPedido.Id = pedidoId;

            // 4. Insertar ITEMS (multi-insert)
            await _pedidoRepo.CrearItemsPedidoAsync(nuevoPedido.Items, pedidoId, conexion, transaction, ct);

            // 5. Insertar HISTORIAL
            await _pedidoRepo.CrearHistorialAsync(pedidoId, EstadoPedido.EsperaConfirmacion,
                "Creación de pedido. Esperando confirmación.", conexion, transaction, ct);

            await transaction.CommitAsync(ct);
        }
        catch (ArgumentException)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "[PEDIDOSERVICE] Error al crear pedido en la base de datos.");
            throw;
        }

        return nuevoPedido;
    }

    public async Task CambiarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, CancellationToken ct = default)
    {
        var estadoActual = await _pedidoRepo.ObtenerEstadoActualAsync(pedidoId, ct);
        if (estadoActual is null)
        {
            throw new PedidoNoEncontradoException(pedidoId);
        }

        // Idempotencia: pedir el estado en el que ya está no genera cambios ni historial.
        if (estadoActual == nuevoEstado)
        {
            _logger.LogInformation("[PEDIDOSERVICE] Pedido {Id} ya está en {Estado}. Sin cambios.", pedidoId, nuevoEstado);
            return;
        }

        if (!TransicionesValidas.TryGetValue(estadoActual.Value, out var estadoEsperado) || estadoEsperado != nuevoEstado)
        {
            throw new TransicionInvalidaException(estadoActual.Value, nuevoEstado);
        }

        await ActualizarEstadoAsync(pedidoId, nuevoEstado, $"Cambio de estado de {estadoActual} a {nuevoEstado}.", ct);
    }

    public async Task ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, string observacion, CancellationToken ct = default)
    {
        using var conexion = _ado.GetDbConnection();
        await conexion.OpenAsync(ct);
        using var transaction = await conexion.BeginTransactionAsync(ct);

        try
        {
            _logger.LogInformation("[PEDIDOSERVICE] Transicionando pedido {Id} a {Estado} - {Obs}", pedidoId, nuevoEstado, observacion);

            // Actualizar pedido
            await _pedidoRepo.ActualizarEstadoAsync(pedidoId, nuevoEstado, conexion, transaction, ct);

            // Insertar historial
            await _pedidoRepo.CrearHistorialAsync(pedidoId, nuevoEstado, observacion, conexion, transaction, ct);

            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "[PEDIDOSERVICE] Error al actualizar estado del pedido {Id}.", pedidoId);
            throw;
        }
    }

    public async Task<Pedido?> GetPedidoByIdAsync(int id, CancellationToken ct = default)
    {
        return await _pedidoRepo.ObtenerPedidoPorIdAsync(id, ct);
    }

    public async Task<IEnumerable<Pedido>> GetPedidosAsync(EstadoPedido? estado = null, CancellationToken ct = default)
    {
        return await _pedidoRepo.ObtenerPedidosAsync(estado, ct);
    }
}