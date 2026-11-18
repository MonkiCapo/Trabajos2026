using Core.Pizzeria.Servicios.Enum;

namespace Tests.Pizzeria;

public class TransicionesEstadoPedidoTests
{
    [Theory]
    [InlineData(EstadoPedido.EsperaConfirmacion, EstadoPedido.EnPreparacion)]
    [InlineData(EstadoPedido.EsperaConfirmacion, EstadoPedido.Cancelado)]
    [InlineData(EstadoPedido.EnPreparacion, EstadoPedido.EnViaje)]
    [InlineData(EstadoPedido.EnPreparacion, EstadoPedido.Cancelado)]
    [InlineData(EstadoPedido.EnViaje, EstadoPedido.Entregado)]
    [InlineData(EstadoPedido.EnViaje, EstadoPedido.Cancelado)]
    public void EsValida_AceptaLasTransicionesDelFlujo(EstadoPedido actual, EstadoPedido siguiente)
    {
        Assert.True(TransicionesEstadoPedido.EsValida(actual, siguiente));
    }

    [Theory]
    [InlineData(EstadoPedido.EsperaConfirmacion, EstadoPedido.EsperaConfirmacion)]
    [InlineData(EstadoPedido.EsperaConfirmacion, EstadoPedido.Entregado)]
    [InlineData(EstadoPedido.EnPreparacion, EstadoPedido.EsperaConfirmacion)]
    [InlineData(EstadoPedido.EnViaje, EstadoPedido.EnPreparacion)]
    [InlineData(EstadoPedido.EnViaje, EstadoPedido.EsperaConfirmacion)]
    [InlineData(EstadoPedido.Entregado, EstadoPedido.Cancelado)]
    [InlineData(EstadoPedido.Entregado, EstadoPedido.EnViaje)]
    [InlineData(EstadoPedido.Cancelado, EstadoPedido.EsperaConfirmacion)]
    public void EsValida_RechazaRetrocesosYTransicionesFueraDelFlujo(EstadoPedido actual, EstadoPedido siguiente)
    {
        Assert.False(TransicionesEstadoPedido.EsValida(actual, siguiente));
    }

    [Fact]
    public void SiguientesValidos_DeEsperaConfirmacion_SonPreparacionOCancelado()
    {
        var validos = TransicionesEstadoPedido.SiguientesValidos(EstadoPedido.EsperaConfirmacion).ToList();

        Assert.Equal(2, validos.Count);
        Assert.Contains(EstadoPedido.EnPreparacion, validos);
        Assert.Contains(EstadoPedido.Cancelado, validos);
    }

    [Fact]
    public void SiguientesValidos_DeEstadosFinales_EsVacio()
    {
        Assert.Empty(TransicionesEstadoPedido.SiguientesValidos(EstadoPedido.Entregado));
        Assert.Empty(TransicionesEstadoPedido.SiguientesValidos(EstadoPedido.Cancelado));
    }

    [Fact]
    public void Describir_DeEstadoFinal_DiceQueNoAdmiteMasTransiciones()
    {
        var texto = TransicionesEstadoPedido.Describir(EstadoPedido.Entregado);

        Assert.Contains("estado final", texto);
        Assert.Contains("no admite mas transiciones", texto);
    }

    [Fact]
    public void Describir_DeEstadoIntermedio_NombraLosPosibles()
    {
        var texto = TransicionesEstadoPedido.Describir(EstadoPedido.EsperaConfirmacion);

        Assert.Contains(nameof(EstadoPedido.EnPreparacion), texto);
        Assert.Contains(nameof(EstadoPedido.Cancelado), texto);
    }
}
