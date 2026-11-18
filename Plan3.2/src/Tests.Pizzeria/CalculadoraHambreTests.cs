using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;

namespace Tests.Pizzeria;

public class CalculadoraHambreTests
{
    private static readonly List<Tamanio> TamaniosEstandar =
    [
        new Tamanio { Id = 1, Nombre = "Grande", Multiplicador = 1.00m },
        new Tamanio { Id = 2, Nombre = "Mediano", Multiplicador = 0.75m },
        new Tamanio { Id = 3, Nombre = "Chico", Multiplicador = 0.50m }
    ];

    [Theory]
    [InlineData("Grande", 8)]
    [InlineData("Mediano", 6)]
    [InlineData("Chico", 4)]
    public void PorcionesPorTamanio_SigueElMultiplicador(string nombre, int esperado)
    {
        var tamanio = TamaniosEstandar.First(t => t.Nombre == nombre);

        Assert.Equal(esperado, CalculadoraHambre.PorcionesPorTamanio(tamanio));
    }

    [Fact]
    public void PorcionesPorTamanio_MultiplicadorChico_NuncaDaCero()
    {
        var tamanio = new Tamanio { Id = 9, Nombre = "Mini", Multiplicador = 0.10m };

        Assert.Equal(1, CalculadoraHambre.PorcionesPorTamanio(tamanio));
    }

    [Theory]
    [InlineData(NivelHambre.Poco, 2)]
    [InlineData(NivelHambre.Normal, 3)]
    [InlineData(NivelHambre.Mucho, 4)]
    public void PorcionesPorPersona_Adulto(NivelHambre hambre, int esperado)
    {
        Assert.Equal(esperado, CalculadoraHambre.PorcionesPorPersona(hambre, esNino: false));
    }

    [Theory]
    [InlineData(NivelHambre.Poco, 1)]
    [InlineData(NivelHambre.Normal, 2)]
    [InlineData(NivelHambre.Mucho, 2)]
    public void PorcionesPorPersona_Nino_ComeMenos(NivelHambre hambre, int esperado)
    {
        Assert.Equal(esperado, CalculadoraHambre.PorcionesPorPersona(hambre, esNino: true));
    }

    [Fact]
    public void TotalPorciones_CombinaAdultosYNinos()
    {
        // 4 adultos con mucha hambre (16) + 2 ninos normales (4) = 20
        Assert.Equal(20, CalculadoraHambre.TotalPorciones(4, 2, NivelHambre.Mucho));
    }

    [Fact]
    public void Sugerir_CuatroAdultosNormales_DaGrandeYChico()
    {
        // 12 porciones = 1 Grande (8) + 1 Chico (4), sin desperdicio.
        var resultado = CalculadoraHambre.Sugerir(4, 0, NivelHambre.Normal, TamaniosEstandar);

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Grande", resultado[0].Tamanio);
        Assert.Equal(1, resultado[0].Cantidad);
        Assert.Equal("Chico", resultado[1].Tamanio);
        Assert.Equal(1, resultado[1].Cantidad);
    }

    [Fact]
    public void Sugerir_DosAdultosNormales_DaMedianoExacto()
    {
        // 6 porciones justas para una Mediano.
        var resultado = CalculadoraHambre.Sugerir(2, 0, NivelHambre.Normal, TamaniosEstandar);

        Assert.Single(resultado);
        Assert.Equal("Mediano", resultado[0].Tamanio);
        Assert.Equal(1, resultado[0].Cantidad);
    }

    [Fact]
    public void Sugerir_UnaPersonaConPocaHambre_DaChico()
    {
        // 2 porciones: la Chico (4) es lo mas chico disponible.
        var resultado = CalculadoraHambre.Sugerir(1, 0, NivelHambre.Poco, TamaniosEstandar);

        Assert.Single(resultado);
        Assert.Equal("Chico", resultado[0].Tamanio);
        Assert.Equal(1, resultado[0].Cantidad);
    }

    [Fact]
    public void Sugerir_DiezAdultosConMuchaHambre_DaCincoGrandes()
    {
        // 40 porciones exactas con 5 Grandes.
        var resultado = CalculadoraHambre.Sugerir(10, 0, NivelHambre.Mucho, TamaniosEstandar);

        Assert.Single(resultado);
        Assert.Equal("Grande", resultado[0].Tamanio);
        Assert.Equal(5, resultado[0].Cantidad);
    }

    [Fact]
    public void Sugerir_CincoAdultosNormales_DaDosGrandes()
    {
        // 15 porciones = 1 Grande + resto 7: ningun tamanio mas chico alcanza
        // (Mediano 6 < 7), asi que se suma otra Grande.
        var resultado = CalculadoraHambre.Sugerir(5, 0, NivelHambre.Normal, TamaniosEstandar);

        Assert.Single(resultado);
        Assert.Equal("Grande", resultado[0].Tamanio);
        Assert.Equal(2, resultado[0].Cantidad);
    }

    [Fact]
    public void Sugerir_NinosSeSumanALasPorciones()
    {
        // 2 adultos normales (6) + 4 ninos normales (8) = 14 porciones
        // = 1 Grande (8) + resto 6 cubierto por una Mediano.
        var resultado = CalculadoraHambre.Sugerir(2, 4, NivelHambre.Normal, TamaniosEstandar);

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Grande", resultado[0].Tamanio);
        Assert.Equal(1, resultado[0].Cantidad);
        Assert.Equal("Mediano", resultado[1].Tamanio);
        Assert.Equal(1, resultado[1].Cantidad);
    }

    [Fact]
    public void Sugerir_SeAdaptaAMultiplicadoresEditadosPorElAdmin()
    {
        // Si el admin cambia Mediano a x0.90, rinde 7 porciones: 6 porciones
        // se cubren con una Mediano en lugar de Grande+Chico.
        var tamanios = new List<Tamanio>
        {
            new() { Id = 1, Nombre = "Grande", Multiplicador = 1.00m },
            new() { Id = 2, Nombre = "Mediano", Multiplicador = 0.90m }
        };

        var resultado = CalculadoraHambre.Sugerir(2, 0, NivelHambre.Normal, tamanios);

        Assert.Single(resultado);
        Assert.Equal("Mediano", resultado[0].Tamanio);
        Assert.Equal(1, resultado[0].Cantidad);
    }

    [Fact]
    public void Sugerir_SinPersonas_DevuelveVacio()
    {
        var resultado = CalculadoraHambre.Sugerir(0, 0, NivelHambre.Normal, TamaniosEstandar);

        Assert.Empty(resultado);
    }

    [Fact]
    public void Sugerir_SinTamanios_DevuelveVacio()
    {
        var resultado = CalculadoraHambre.Sugerir(4, 0, NivelHambre.Normal, new List<Tamanio>());

        Assert.Empty(resultado);
    }
}
