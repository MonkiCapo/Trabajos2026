using FluentValidation;
using Moq;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Services.Pizzeria.Services;
using Services.Pizzeria.Validations;

namespace Tests.Pizzeria;

public class TamanioServiceTests
{
    private readonly Mock<ITamanioRepositorio> _tamanioRepo = new();
    private readonly TamanioService _servicio;

    public TamanioServiceTests()
    {
        _servicio = new TamanioService(_tamanioRepo.Object, new TamanioUpdateRequestValidator());
    }

    [Fact]
    public async Task ObtenerTamaniosAsync_DevuelveLosDelRepositorio()
    {
        var esperados = new List<Tamanio>
        {
            new() { Id = 1, Nombre = "Grande", Multiplicador = 1.00m }
        };
        _tamanioRepo.Setup(r => r.ObtenerTamaniosAsync()).ReturnsAsync(esperados);

        var resultado = await _servicio.ObtenerTamaniosAsync();

        Assert.Same(esperados, resultado);
    }

    [Fact]
    public async Task ActualizarMultiplicador_Valido_ActualizaYDevuelveElTamanio()
    {
        _tamanioRepo.Setup(r => r.ActualizarMultiplicadorAsync(2, 0.80m)).ReturnsAsync(true);
        _tamanioRepo.Setup(r => r.ObtenerTamanioPorIdAsync(2))
            .ReturnsAsync(new Tamanio { Id = 2, Nombre = "Mediano", Multiplicador = 0.80m });

        var resultado = await _servicio.ActualizarMultiplicadorAsync(2, new TamanioUpdateRequest
        {
            Multiplicador = 0.80m
        });

        Assert.NotNull(resultado);
        Assert.Equal("Mediano", resultado.Nombre);
        Assert.Equal(0.80m, resultado.Multiplicador);
    }

    [Fact]
    public async Task ActualizarMultiplicador_Invalido_LanzaValidacionSinTocarElRepo()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _servicio.ActualizarMultiplicadorAsync(2,
            new TamanioUpdateRequest { Multiplicador = 9.99m }));

        _tamanioRepo.Verify(r => r.ActualizarMultiplicadorAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarMultiplicador_IdInexistente_DevuelveNull()
    {
        _tamanioRepo.Setup(r => r.ActualizarMultiplicadorAsync(99, 0.75m)).ReturnsAsync(false);

        var resultado = await _servicio.ActualizarMultiplicadorAsync(99, new TamanioUpdateRequest
        {
            Multiplicador = 0.75m
        });

        Assert.Null(resultado);
    }
}
