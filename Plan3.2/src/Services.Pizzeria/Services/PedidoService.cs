using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Services.Pizzeria.Services;

public class PedidoService : IPedidoService
{
    private readonly IClienteRepositorio _clienteRepo;
    private readonly IPedidoRepositorio _pedidoRepo;
    private readonly IPizzaRepositorio _pizzaRepo;
    private readonly IAdo _ado;
    private readonly IValidator<CheckoutRequest> _validadorCheckout;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        IClienteRepositorio clienteRepo,
        IPedidoRepositorio pedidoRepo,
        IPizzaRepositorio pizzaRepo,
        IAdo ado,
        IValidator<CheckoutRequest> validadorCheckout,
        ILogger<PedidoService> logger)
    {
        _clienteRepo = clienteRepo;
        _pedidoRepo = pedidoRepo;
        _pizzaRepo = pizzaRepo;
        _ado = ado;
        _validadorCheckout = validadorCheckout;
        _logger = logger;
    }

    /// <summary>
    /// Resuelve los precios desde la base, calcula el total y escribe PEDIDO + ITEM_PEDIDO + HISTORIAL.
    /// Es la parte comun entre el alta de pedido y el checkout.
    /// </summary>
    private async Task PersistirPedidoAsync(Pedido pedido, IDbConnection conexion, IDbTransaction transaction)
    {
        // 1. Resolver precios de pizzas por nombre y calcular total
        //    Los precios NUNCA se confian: se vuelven a consultar en la base.
        decimal calculatedTotal = 0;

        foreach (var item in pedido.Items)
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

        pedido.Total = calculatedTotal;
        pedido.Estado = EstadoPedido.EsperaConfirmacion;
        pedido.FechaCreacion = DateTime.UtcNow;
        pedido.FechaActualizacion = DateTime.UtcNow;

        // 2. Insertar PEDIDO
        var pedidoId = await _pedidoRepo.CrearPedidoAsync(pedido, conexion, transaction);
        pedido.Id = pedidoId;

        // 3. Insertar ITEMS
        await _pedidoRepo.CrearItemsPedidoAsync(pedido.Items, pedidoId, conexion, transaction);

        // 4. Insertar HISTORIAL
        await _pedidoRepo.CrearHistorialAsync(pedidoId, EstadoPedido.EsperaConfirmacion,
            "Creación de pedido", conexion, transaction);
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

            await PersistirPedidoAsync(nuevoPedido, conexion, transaction);

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

    /// <summary>
    /// Cierra un pedido con los datos de contacto y las pizzas elegidas.
    /// Si el cliente todavia no existe, lo da de alta en la misma transaccion.
    /// </summary>
    public async Task<Pedido> CrearPedidoConDatosAsync(CheckoutRequest request)
    {
        var validationResult = await _validadorCheckout.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        try
        {
            // 1. Find-or-create del cliente (la lectura corre dentro de la transaccion)
            var cliente = await _clienteRepo.ObtenerClientePorEmailAsync(request.Email, conexion, transaction);

            if (cliente == null)
            {
                cliente = new Cliente
                {
                    Nombre = request.Nombre,
                    Email = request.Email,
                    Telefono = request.Telefono,
                    Direccion = request.Direccion
                };

                cliente.Id = await _clienteRepo.AgregarClienteAsync(cliente, conexion, transaction);
                _logger.LogInformation("[PEDIDOSERVICE] Cliente {Id} dado de alta durante el checkout.", cliente.Id);
            }
            else
            {
                _logger.LogInformation("[PEDIDOSERVICE] Cliente existente {Id} reutilizado en el checkout.", cliente.Id);
            }

            // 2. Armar el pedido con las pizzas elegidas
            var pedido = new Pedido
            {
                ClienteId = cliente.Id,
                Items = request.Items.Select(i => new ItemPedido
                {
                    PizzaNombre = i.PizzaNombre,
                    Cantidad = i.Cantidad
                }).ToList()
            };

            // 3. Precios, total e inserts
            await PersistirPedidoAsync(pedido, conexion, transaction);

            transaction.Commit();

            _logger.LogInformation("[PEDIDOSERVICE] Pedido {Id} creado en estado {Estado} (total: {Total}).",
                pedido.Id, pedido.Estado, pedido.Total);

            return pedido;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "[PEDIDOSERVICE] Error al crear el pedido desde el checkout.");
            throw;
        }
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
