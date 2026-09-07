using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Api.Pizzeria.Services;

public class PedidoService : IPedidoService
{
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

        _logger.LogInformation("[PEDIDOSERVICE] Pedido {Id} creado en estado {Estado}.", nuevoPedido.Id, nuevoPedido.Estado);

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
    }

    public async Task<Pedido?> GetPedidoByIdAsync(int id)
    {
        return await _pedidoRepo.ObtenerPedidoPorIdAsync(id);
    }
}
