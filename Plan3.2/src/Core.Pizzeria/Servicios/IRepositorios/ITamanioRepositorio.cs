using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface ITamanioRepositorio
{
    Task<IEnumerable<Tamanio>> ObtenerTamaniosAsync();
    Task<IEnumerable<Tamanio>> ObtenerTamaniosAsync(IDbConnection conexion, IDbTransaction transaction);
    Task<Tamanio?> ObtenerTamanioPorIdAsync(int id);
    Task<bool> ActualizarMultiplicadorAsync(int id, decimal multiplicador);
}
