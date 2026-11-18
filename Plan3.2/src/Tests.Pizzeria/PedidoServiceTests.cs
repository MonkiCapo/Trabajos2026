using System.Data;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;
using Services.Pizzeria.Services;
using Services.Pizzeria.Validations;

namespace Tests.Pizzeria;

public class PedidoServiceTests
{
    private sealed class FechaFijaTimeProvider(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    private readonly Mock<IClienteRepositorio> _clienteRepo = new();
    private readonly Mock<IPedidoRepositorio> _pedidoRepo = new();
    private readonly Mock<IPizzaRepositorio> _pizzaRepo = new();
    private readonly Mock<IIngredienteRepositorio> _ingredienteRepo = new();
    private readonly Mock<ITamanioRepositorio> _tamanioRepo = new();
    private readonly Mock<IAdo> _ado = new();
    private readonly Mock<IDbConnection> _conexion = new() { DefaultValue = DefaultValue.Mock };
    private readonly Mock<IDbTransaction> _transaccion = new() { DefaultValue = DefaultValue.Mock };

    private static readonly DateTimeOffset FechaFija = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    private readonly TimeProvider _timeProvider = new FechaFijaTimeProvider(FechaFija);

    private static readonly List<Tamanio> TamaniosBase =
    [
        new Tamanio { Id = 1, Nombre = "Grande", Multiplicador = 1.00m },
        new Tamanio { Id = 2, Nombre = "Mediano", Multiplicador = 0.75m },
        new Tamanio { Id = 3, Nombre = "Chico", Multiplicador = 0.50m }
    ];

    private readonly List<Ingrediente> _catalogoIngredientes =
    [
        new Ingrediente { Id = 101, Nombre = "Muzzarella" },
        new Ingrediente { Id = 102, Nombre = "Salsa" },
        new Ingrediente { Id = 103, Nombre = "Aceitunas" }
    ];

    private PedidoService CrearServicio() => new(
        _clienteRepo.Object,
        _pedidoRepo.Object,
        _pizzaRepo.Object,
        _ingredienteRepo.Object,
        _tamanioRepo.Object,
        _ado.Object,
        new PedidoRequestValidator(),
        new CheckoutRequestValidator(),
        NullLogger<PedidoService>.Instance,
        _timeProvider);

    private void ConfigurarInfraestructura()
    {
        _conexion.Setup(c => c.BeginTransaction()).Returns(_transaccion.Object);
        _ado.Setup(a => a.GetDbConnection()).Returns(_conexion.Object);

        _tamanioRepo.Setup(r => r.ObtenerTamaniosAsync(_conexion.Object, _transaccion.Object))
            .ReturnsAsync(TamaniosBase);

        _pedidoRepo.Setup(r => r.CrearPedidoAsync(It.IsAny<Pedido>(), _conexion.Object, _transaccion.Object))
            .ReturnsAsync(100);

        // El repositorio deja los ids de los items seteados, como hace el real.
        _pedidoRepo.Setup(r => r.CrearItemsPedidoAsync(It.IsAny<List<ItemPedido>>(), It.IsAny<int>(), _conexion.Object, _transaccion.Object))
            .Callback((List<ItemPedido> items, int pedidoId, IDbConnection c, IDbTransaction t) =>
            {
                for (var indice = 0; indice < items.Count; indice++)
                {
                    items[indice].Id = 1000 + indice;
                }
            })
            .Returns(Task.CompletedTask);

        _pedidoRepo.Setup(r => r.CrearAjustesAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), _conexion.Object, _transaccion.Object))
            .Returns(Task.CompletedTask);

        _pedidoRepo.Setup(r => r.CrearHistorialAsync(It.IsAny<int>(), It.IsAny<EstadoPedido>(), It.IsAny<string>(), _conexion.Object, _transaccion.Object))
            .Returns(Task.CompletedTask);
    }

    private void ConfigurarCliente()
    {
        _clienteRepo.Setup(r => r.ObtenerClientePorIdAsync(1, _conexion.Object, _transaccion.Object))
            .ReturnsAsync(new Cliente { Id = 1, Nombre = "Juan", Email = "juan@test.com", Telefono = "11", Direccion = "Calle 1" });

        _clienteRepo.Setup(r => r.ActualizarClienteAsync(It.IsAny<Cliente>(), 1, _conexion.Object, _transaccion.Object))
            .ReturnsAsync(true);
    }

    private void ConfigurarCatalogo(Pizza pizza, IDictionary<int, List<string>>? ingredientesDePizza = null)
    {
        _pizzaRepo.Setup(r => r.ObtenerPizzasPorNombresAsync(It.IsAny<IEnumerable<string>>(), _conexion.Object, _transaccion.Object))
            .ReturnsAsync(new List<Pizza> { pizza });

        _pizzaRepo.Setup(r => r.ObtenerIngredientesDePizzasAsync(It.IsAny<IEnumerable<int>>(), _conexion.Object, _transaccion.Object))
            .ReturnsAsync(ingredientesDePizza ?? new Dictionary<int, List<string>>());

        _ingredienteRepo.Setup(r => r.ObtenerPorNombresAsync(It.IsAny<IEnumerable<string>>(), _conexion.Object, _transaccion.Object))
            .ReturnsAsync((IEnumerable<string> nombres, IDbConnection c, IDbTransaction t) =>
                _catalogoIngredientes
                    .Where(i => nombres.Contains(i.Nombre, StringComparer.OrdinalIgnoreCase))
                    .ToList());
    }

    private static Pizza PizzaMuzzarella() => new()
    {
        Id = 10,
        Nombre = "Muzzarella",
        Precio = 10m,
        Ingredientes = new List<string> { "Muzzarella", "Salsa" }
    };

    private static CheckoutRequest Checkout(params ItemRequest[] items) => new()
    {
        Nombre = "Juan",
        Telefono = "11 5555-4444",
        Direccion = "Calle 1",
        Items = items.ToList()
    };

    [Fact]
    public async Task CheckoutSinAjustes_CalculaTotalBase()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 2 }));

        Assert.Equal(20m, pedido.Total);
        Assert.Equal(10m, pedido.Items[0].PrecioUnitario);
        Assert.Equal(EstadoPedido.EsperaConfirmacion, pedido.Estado);
        Assert.Equal(100, pedido.Id);

        _transaccion.Verify(t => t.Commit(), Times.Once);
        _transaccion.Verify(t => t.Rollback(), Times.Never);
    }

    [Fact]
    public async Task Extras_SumanRecargoAlPrecioYGuardanLosIds()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest
            {
                PizzaNombre = "Muzzarella",
                Cantidad = 1,
                Extras = new List<string> { "Muzzarella", "Salsa" }
            }));

        // 10 base + 2 extras x $0.50 = 11
        Assert.Equal(11m, pedido.Items[0].PrecioUnitario);
        Assert.Equal(11m, pedido.Total);

        _pedidoRepo.Verify(r => r.CrearAjustesAsync(
            1000,
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 101, 102 })),
            "extra",
            _conexion.Object,
            _transaccion.Object), Times.Once);
    }

    [Fact]
    public async Task Quitar_NoCambiaElPrecio_YGuardaElAjuste()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella(), new Dictionary<int, List<string>>
        {
            [10] = new List<string> { "Muzzarella", "Salsa" }
        });

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest
            {
                PizzaNombre = "Muzzarella",
                Cantidad = 1,
                Quitar = new List<string> { "Salsa" }
            }));

        Assert.Equal(10m, pedido.Items[0].PrecioUnitario);
        Assert.Equal(10m, pedido.Total);

        _pedidoRepo.Verify(r => r.CrearAjustesAsync(
            1000,
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 102 })),
            "quitar",
            _conexion.Object,
            _transaccion.Object), Times.Once);
    }

    [Fact]
    public async Task CantidadYExtras_SeCombinanEnElTotal()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest
            {
                PizzaNombre = "Muzzarella",
                Cantidad = 3,
                Extras = new List<string> { "Aceitunas" }
            }));

        // (10 + 0.50) x 3 = 31.50
        Assert.Equal(31.5m, pedido.Total);
        Assert.Equal(10.5m, pedido.Items[0].PrecioUnitario);
    }

    [Fact]
    public async Task QuitarIngredienteAjenoALaPizza_RechazaYRevierte()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella(), new Dictionary<int, List<string>>
        {
            [10] = new List<string> { "Muzzarella", "Salsa" }
        });

        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest
            {
                PizzaNombre = "Muzzarella",
                Cantidad = 1,
                Quitar = new List<string> { "Aceitunas" }
            })));

        _transaccion.Verify(t => t.Rollback(), Times.Once);
        _transaccion.Verify(t => t.Commit(), Times.Never);
        _pedidoRepo.Verify(r => r.CrearPedidoAsync(It.IsAny<Pedido>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Never);
    }

    [Fact]
    public async Task ExtraInexistenteEnElCatalogo_RechazaYRevierte()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest
            {
                PizzaNombre = "Muzzarella",
                Cantidad = 1,
                Extras = new List<string> { "Ananá" }
            })));

        _transaccion.Verify(t => t.Rollback(), Times.Once);
        _transaccion.Verify(t => t.Commit(), Times.Never);
    }

    [Fact]
    public async Task PizzaInexistente_LanzaArgumentExceptionYRevierte()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Napolitana", Cantidad = 1 })));

        _transaccion.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public async Task ClienteDelTokenInexistente_LanzaArgumentExceptionYRevierte()
    {
        ConfigurarInfraestructura();
        ConfigurarCatalogo(PizzaMuzzarella());

        _clienteRepo.Setup(r => r.ObtenerClientePorIdAsync(99, _conexion.Object, _transaccion.Object))
            .ReturnsAsync((Cliente?)null);

        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearPedidoConDatosAsync(99, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1 })));

        _transaccion.Verify(t => t.Rollback(), Times.Once);
    }

    [Fact]
    public async Task CheckoutEstructuralmenteInvalido_NoAbreConexion()
    {
        ConfigurarInfraestructura();

        var servicio = CrearServicio();

        // Cantidad 0: lo atrapa el validador antes de tocar la base.
        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 0 })));

        _ado.Verify(a => a.GetDbConnection(), Times.Never);
    }

    [Fact]
    public async Task TamanioMediano_AplicaMultiplicadorAlPrecio()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 2, Tamanio = "Mediano" }));

        // 10 x 0.75 = 7.50 por unidad; x2 = 15.00. El nombre queda guardado.
        Assert.Equal(7.5m, pedido.Items[0].PrecioUnitario);
        Assert.Equal(15m, pedido.Total);
        Assert.Equal("Mediano", pedido.Items[0].Tamanio);
    }

    [Fact]
    public async Task TamanioChico_ConExtras_CalculaElPrecioFinal()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest
            {
                PizzaNombre = "Muzzarella",
                Cantidad = 1,
                Tamanio = "Chico",
                Extras = new List<string> { "Aceitunas" }
            }));

        // 10 x 0.50 = 5.00 + 0.50 de extra = 5.50
        Assert.Equal(5.5m, pedido.Items[0].PrecioUnitario);
    }

    [Fact]
    public async Task TamanioConDecimales_RedondeaAwayFromZero()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(new Pizza
        {
            Id = 10,
            Nombre = "Muzzarella",
            Precio = 9.99m,
            Ingredientes = new List<string> { "Muzzarella", "Salsa" }
        });

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1, Tamanio = "Mediano" }));

        // 9.99 x 0.75 = 7.4925 -> 7.49 (MidpointRounding.AwayFromZero)
        Assert.Equal(7.49m, pedido.Items[0].PrecioUnitario);
    }

    [Fact]
    public async Task TamanioVacio_UsaGrandePorDefecto()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1 }));

        Assert.Equal(10m, pedido.Items[0].PrecioUnitario);
        Assert.Equal("Grande", pedido.Items[0].Tamanio);
    }

    [Fact]
    public async Task TamanioConDistintaCapitalizacion_SeAceptaYCanoniza()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1, Tamanio = "mediano" }));

        Assert.Equal("Mediano", pedido.Items[0].Tamanio);
        Assert.Equal(7.5m, pedido.Items[0].PrecioUnitario);
    }

    [Fact]
    public async Task TamanioInexistente_RechazaYRevierte()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1, Tamanio = "Gigante" })));

        _transaccion.Verify(t => t.Rollback(), Times.Once);
        _transaccion.Verify(t => t.Commit(), Times.Never);
        _pedidoRepo.Verify(r => r.CrearPedidoAsync(It.IsAny<Pedido>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Fechas_SalenDelTimeProviderInyectado()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1 }));

        Assert.Equal(FechaFija.DateTime, pedido.FechaCreacion);
        Assert.Equal(FechaFija.DateTime, pedido.FechaActualizacion);
    }

    [Fact]
    public async Task Checkout_ActualizaLosDatosDeContactoDelCliente()
    {
        ConfigurarInfraestructura();
        ConfigurarCliente();
        ConfigurarCatalogo(PizzaMuzzarella());

        var servicio = CrearServicio();
        await servicio.CrearPedidoConDatosAsync(1, Checkout(
            new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1 }));

        _clienteRepo.Verify(r => r.ActualizarClienteAsync(
            It.Is<Cliente>(c => c.Nombre == "Juan" && c.Telefono == "11 5555-4444" && c.Direccion == "Calle 1"),
            1,
            _conexion.Object,
            _transaccion.Object), Times.Once);
    }

    [Fact]
    public async Task CrearPedidoAsync_ResuelveElEmailAClienteYCalculaTotal()
    {
        ConfigurarInfraestructura();
        ConfigurarCatalogo(PizzaMuzzarella());

        _clienteRepo.Setup(r => r.ObtenerClientePorEmailAsync("juan@test.com", _conexion.Object, _transaccion.Object))
            .ReturnsAsync(new Cliente { Id = 1, Nombre = "Juan", Email = "juan@test.com" });

        var servicio = CrearServicio();
        var pedido = await servicio.CrearPedidoAsync(new PedidoRequest
        {
            ClienteEmail = "juan@test.com",
            Items = new List<ItemRequest>
            {
                new() { PizzaNombre = "Muzzarella", Cantidad = 3 }
            }
        });

        Assert.Equal(1, pedido.ClienteId);
        Assert.Equal(30m, pedido.Total);
        _transaccion.Verify(t => t.Commit(), Times.Once);
    }

    [Fact]
    public async Task CrearPedidoAsync_EmailSinCliente_LanzaArgumentException()
    {
        ConfigurarInfraestructura();
        ConfigurarCatalogo(PizzaMuzzarella());

        _clienteRepo.Setup(r => r.ObtenerClientePorEmailAsync("nadie@test.com", _conexion.Object, _transaccion.Object))
            .ReturnsAsync((Cliente?)null);

        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearPedidoAsync(new PedidoRequest
        {
            ClienteEmail = "nadie@test.com",
            Items = new List<ItemRequest> { new() { PizzaNombre = "Muzzarella", Cantidad = 1 } }
        }));

        _transaccion.Verify(t => t.Rollback(), Times.Once);
    }
}
