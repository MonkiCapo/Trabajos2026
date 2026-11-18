using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IService;

public interface ITamanioService
{
    Task<IEnumerable<Tamanio>> ObtenerTamaniosAsync();
    Task<Tamanio?> ActualizarMultiplicadorAsync(int id, TamanioUpdateRequest request);
}
