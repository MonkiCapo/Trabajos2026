using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class TamanioService : ITamanioService
{
    private readonly ITamanioRepositorio _tamanioRepo;
    private readonly IValidator<TamanioUpdateRequest> _validador;

    public TamanioService(ITamanioRepositorio tamanioRepo, IValidator<TamanioUpdateRequest> validador)
    {
        _tamanioRepo = tamanioRepo;
        _validador = validador;
    }

    public async Task<IEnumerable<Tamanio>> ObtenerTamaniosAsync()
    {
        return await _tamanioRepo.ObtenerTamaniosAsync();
    }

    public async Task<Tamanio?> ActualizarMultiplicadorAsync(int id, TamanioUpdateRequest request)
    {
        var resultado = await _validador.ValidateAsync(request);
        if (!resultado.IsValid)
        {
            throw new ValidationException(resultado.Errors);
        }

        var actualizado = await _tamanioRepo.ActualizarMultiplicadorAsync(id, request.Multiplicador);
        if (!actualizado)
        {
            return null;
        }

        return await _tamanioRepo.ObtenerTamanioPorIdAsync(id);
    }
}
