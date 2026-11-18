using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.Excepciones;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Services.Pizzeria.Services;

public class PedidoService : IPedidoService
{
    private readonly IClienteRepositorio _clienteRepo;
    private readonly IPedidoRepositorio _pedidoRepo;
    private readonly IPizzaRepositorio _pizzaRepo;
    private readonly IIngredienteRepositorio _ingredienteRepo;
    private readonly ITamanioRepositorio _tamanioRepo;
    private readonly IAdo _ado;
    private readonly IValidator<PedidoRequest> _validadorPedido;
    private readonly IValidator<CheckoutRequest> _validadorCheckout;
    private readonly ILogger<PedidoService> _logger;
    private readonly TimeProvider _timeProvider;

    public PedidoService(
        IClienteRepositorio clienteRepo,
        IPedidoRepositorio pedidoRepo,
        IPizzaRepositorio pizzaRepo,
        IIngredienteRepositorio ingredienteRepo,
        ITamanioRepositorio tamanioRepo,
        IAdo ado,
        IValidator<PedidoRequest> validadorPedido,
        IValidator<CheckoutRequest> validadorCheckout,
        ILogger<PedidoService> logger,
        TimeProvider timeProvider)
    {
        _clienteRepo = clienteRepo;
        _pedidoRepo = pedidoRepo;
        _pizzaRepo = pizzaRepo;
        _ingredienteRepo = ingredienteRepo;
        _tamanioRepo = tamanioRepo;
        _ado = ado;
        _validadorPedido = validadorPedido;
        _validadorCheckout = validadorCheckout;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    private async Task PersistirPedidoAsync(Pedido pedido, IDbConnection conexion, IDbTransaction transaction)
    {
        // 1. Resolver precios de pizzas por nombre y calcular total
        //    Los precios NUNCA se confian: se vuelven a consultar en la base.
        //    Una sola consulta con IN sobre la conexion transaccional (sin N+1)
        //    y los precios se leen de un diccionario en memoria.
        var nombres = pedido.Items.Select(i => i.PizzaNombre).ToList();
        var pizzas = await _pizzaRepo.ObtenerPizzasPorNombresAsync(nombres, conexion, transaction);

        // MySQL compara nombres sin distinguir mayusculas: el diccionario igual.
        var pizzasPorNombre = pizzas.ToDictionary(p => p.Nombre, StringComparer.OrdinalIgnoreCase);

        // Tamanios: se leen en la misma transaccion y el precio se recalcula
        // con el multiplicador vigente. El nombre elegido queda guardado en
        // el item, asi los pedidos historicos no cambian si luego se edita
        // el multiplicador. Sin tamanio (o vacio) se asume "Grande".
        var tamanios = await _tamanioRepo.ObtenerTamaniosAsync(conexion, transaction);
        var tamaniosPorNombre = tamanios.ToDictionary(t => t.Nombre, t => t, StringComparer.OrdinalIgnoreCase);

        for (var indice = 0; indice < pedido.Items.Count; indice++)
        {
            var item = pedido.Items[indice];

            if (!pizzasPorNombre.TryGetValue(item.PizzaNombre, out var pizzaInfo))
            {
                throw new ArgumentException($"La pizza \"{item.PizzaNombre}\" no existe en el catalogo.");
            }

            var tamanioElegido = string.IsNullOrWhiteSpace(item.Tamanio) ? "Grande" : item.Tamanio.Trim();
            if (!tamaniosPorNombre.TryGetValue(tamanioElegido, out var tamanioInfo))
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure($"Items[{indice}].Tamanio",
                        $"El tamaño '{item.Tamanio}' no existe. Disponibles: {string.Join(", ", tamanios.Select(t => t.Nombre))}.")
                });
            }

            item.PizzaId = pizzaInfo.Id;
            item.PizzaNombre = pizzaInfo.Nombre;
            item.Tamanio = tamanioInfo.Nombre;
            item.PrecioUnitario = decimal.Round(
                pizzaInfo.Precio * tamanioInfo.Multiplicador, 2, MidpointRounding.AwayFromZero);
        }

        // 2. Ajustes de "Arma tu pizza": se validan contra la BD y se resuelven
        //    a ids de INGREDIENTE dentro de la misma transaccion. Los "quitar"
        //    deben ser ingredientes de esa pizza y los extras deben existir.
        var pizzaIds = pedido.Items.Select(i => i.PizzaId).Distinct().ToList();
        var ingredientesDePizza = await _pizzaRepo.ObtenerIngredientesDePizzasAsync(pizzaIds, conexion, transaction);

        var nombresDeAjustes = pedido.Items
            .SelectMany(i => i.Quitar.Concat(i.Extras))
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ingredientesPorNombre = new Dictionary<string, Ingrediente>(StringComparer.OrdinalIgnoreCase);
        if (nombresDeAjustes.Count > 0)
        {
            var existentes = await _ingredienteRepo.ObtenerPorNombresAsync(nombresDeAjustes, conexion, transaction);
            foreach (var ingrediente in existentes)
            {
                ingredientesPorNombre[ingrediente.Nombre] = ingrediente;
            }
        }

        for (var indice = 0; indice < pedido.Items.Count; indice++)
        {
            var item = pedido.Items[indice];

            item.Quitar = item.Quitar.Select(n => n.Trim()).Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            item.Extras = item.Extras.Select(n => n.Trim()).Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var propios = ingredientesDePizza.TryGetValue(item.PizzaId, out var lista)
                ? lista
                : new List<string>();

            foreach (var nombre in item.Quitar)
            {
                if (!propios.Contains(nombre, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure($"Items[{indice}].Quitar",
                            $"El ingrediente '{nombre}' no esta en la pizza '{item.PizzaNombre}'.")
                    });
                }
            }

            foreach (var nombre in item.Extras)
            {
                if (!ingredientesPorNombre.ContainsKey(nombre))
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure($"Items[{indice}].Extras",
                            $"El ingrediente '{nombre}' no existe en el catalogo.")
                    });
                }
            }

            // Regla de precio: quitar no cambia nada, cada extra suma el recargo.
            item.PrecioUnitario += item.Extras.Count * PreciosAjuste.RecargoExtra;
        }

        pedido.Total = pedido.Items.Sum(i => i.PrecioUnitario * i.Cantidad);
        pedido.Estado = EstadoPedido.EsperaConfirmacion;

        // Las fechas salen del TimeProvider inyectado: en produccion es la
        // hora real del sistema y en los tests se puede fijar.
        var ahora = _timeProvider.GetUtcNow().DateTime;
        pedido.FechaCreacion = ahora;
        pedido.FechaActualizacion = ahora;

        // 3. Insertar PEDIDO
        var pedidoId = await _pedidoRepo.CrearPedidoAsync(pedido, conexion, transaction);
        pedido.Id = pedidoId;

        // 4. Insertar ITEMS (el repositorio deja el Id de cada item seteado)
        await _pedidoRepo.CrearItemsPedidoAsync(pedido.Items, pedidoId, conexion, transaction);

        // 5. Insertar AJUSTES de cada item
        foreach (var item in pedido.Items)
        {
            if (item.Quitar.Count > 0)
            {
                await _pedidoRepo.CrearAjustesAsync(item.Id,
                    item.Quitar.Select(n => ingredientesPorNombre[n].Id).ToList(),
                    "quitar", conexion, transaction);
            }

            if (item.Extras.Count > 0)
            {
                await _pedidoRepo.CrearAjustesAsync(item.Id,
                    item.Extras.Select(n => ingredientesPorNombre[n].Id).ToList(),
                    "extra", conexion, transaction);
            }
        }

        // 6. Insertar HISTORIAL
        await _pedidoRepo.CrearHistorialAsync(pedidoId, EstadoPedido.EsperaConfirmacion,
            "Creación de pedido", conexion, transaction);
    }

    public async Task<Pedido> CrearPedidoAsync(PedidoRequest request)
    {
        var validationResult = await _validadorPedido.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        Pedido nuevoPedido;

        try
        {
            // Resolver email a ID de cliente (la lectura corre dentro de la transaccion)
            var cliente = await _clienteRepo.ObtenerClientePorEmailAsync(request.ClienteEmail, conexion, transaction);
            if (cliente == null)
            {
                throw new ArgumentException($"No se encontro un cliente con el email '{request.ClienteEmail}'.");
            }

            nuevoPedido = new Pedido
            {
                ClienteId = cliente.Id,
                Items = request.Items.Select(i => new ItemPedido
                {
                    PizzaNombre = i.PizzaNombre,
                    Cantidad = i.Cantidad,
                    Tamanio = i.Tamanio,
                    Quitar = i.Quitar,
                    Extras = i.Extras
                }).ToList()
            };

            await PersistirPedidoAsync(nuevoPedido, conexion, transaction);

            transaction.Commit();
        }
        catch (ArgumentException)
        {
            transaction.Rollback();
            throw;
        }
        catch (ValidationException)
        {
            // Ajustes invalidos (quitar/extras): no es un error del sistema.
            transaction.Rollback();
            throw;
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

    public async Task<Pedido> CrearPedidoConDatosAsync(int clienteId, CheckoutRequest request)
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
            // El cliente viene del token JWT (claim ClienteId), no del body:
            // nadie puede pedir a nombre de otro. La lectura corre dentro de
            // la transaccion.
            var cliente = await _clienteRepo.ObtenerClientePorIdAsync(clienteId, conexion, transaction);
            if (cliente == null)
            {
                throw new ArgumentException($"No se encontro el cliente con id {clienteId} asociado al token.");
            }

            // Los datos de contacto del formulario actualizan al cliente
            // logueado (el email no se toca: lo administra la cuenta).
            cliente.Nombre = request.Nombre;
            cliente.Telefono = request.Telefono;
            cliente.Direccion = request.Direccion;
            await _clienteRepo.ActualizarClienteAsync(cliente, cliente.Id, conexion, transaction);

            // 2. Armar el pedido con las pizzas elegidas
            var pedido = new Pedido
            {
                ClienteId = cliente.Id,
                Items = request.Items.Select(i => new ItemPedido
                {
                    PizzaNombre = i.PizzaNombre,
                    Cantidad = i.Cantidad,
                    Tamanio = i.Tamanio,
                    Quitar = i.Quitar,
                    Extras = i.Extras
                }).ToList()
            };

            // 3. Precios, total e inserts
            await PersistirPedidoAsync(pedido, conexion, transaction);

            transaction.Commit();

            _logger.LogInformation("[PEDIDOSERVICE] Pedido {Id} creado en estado {Estado} (total: {Total}).",
                pedido.Id, pedido.Estado, pedido.Total);

            return pedido;
        }
        catch (ArgumentException)
        {
            // Cliente del token inexistente: error del pedido, no del sistema.
            transaction.Rollback();
            throw;
        }
        catch (ValidationException)
        {
            // Ajustes invalidos (quitar/extras): no es un error del sistema.
            transaction.Rollback();
            throw;
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
            // La lectura corre dentro de la transaccion: el estado que valida
            // la maquina y el que condiciona el UPDATE son la misma lectura.
            var pedido = await _pedidoRepo.ObtenerPedidoPorIdAsync(pedidoId, conexion, transaction);

            if (pedido == null)
            {
                throw new ArgumentException($"El pedido con ID {pedidoId} no existe.");
            }

            if (!TransicionesEstadoPedido.EsValida(pedido.Estado, nuevoEstado))
            {
                throw new ArgumentException(
                    $"Transicion invalida: el pedido {pedidoId} esta en {pedido.Estado} "
                    + $"y no puede pasar a {nuevoEstado}. {TransicionesEstadoPedido.Describir(pedido.Estado)}");
            }

            _logger.LogInformation("[PEDIDOSERVICE] Transicionando pedido {Id} de {Actual} a {Nuevo} - {Obs}",
                pedidoId, pedido.Estado, nuevoEstado, observacion);

            // Actualizar pedido: si otro proceso cambio el estado en el medio,
            // el UPDATE condicional no afecta filas y se informa conflicto.
            var actualizado = await _pedidoRepo.ActualizarEstadoAsync(pedidoId, pedido.Estado, nuevoEstado, conexion, transaction);

            if (!actualizado)
            {
                throw new ConflictoEstadoPedidoException(
                    $"El pedido {pedidoId} cambio de estado mientras se procesaba la transicion. Recarga el estado actual e intenta de nuevo.");
            }

            // Insertar historial
            await _pedidoRepo.CrearHistorialAsync(pedidoId, nuevoEstado, observacion, conexion, transaction);

            transaction.Commit();

            _logger.LogInformation("[PEDIDOSERVICE] Pedido {Id} quedo en {Estado}.", pedidoId, nuevoEstado);
        }
        catch (ConflictoEstadoPedidoException)
        {
            transaction.Rollback();
            _logger.LogWarning("[PEDIDOSERVICE] Conflicto de concurrencia al transicionar el pedido {Id}.", pedidoId);
            throw;
        }
        catch (ArgumentException)
        {
            transaction.Rollback();
            throw;
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

    public async Task<IEnumerable<HistorialEstadoPedido>> ObtenerHistorialAsync(int pedidoId)
    {
        var historial = await _pedidoRepo.ObtenerHistorialAsync(pedidoId);

        return historial;
    }
}
