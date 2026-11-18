using System.Collections.Generic;
using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class TamanioRepositorio : DapperRepo, ITamanioRepositorio
{
    private const string SelectTamanios = "SELECT id, nombre, multiplicador FROM TAMANIO";

    public TamanioRepositorio(IAdo ado) : base(ado) { }

    public async Task<IEnumerable<Tamanio>> ObtenerTamaniosAsync()
    {
        using var conexion = NuevaConexion();
        return await conexion.QueryAsync<Tamanio>($"{SelectTamanios} ORDER BY id;");
    }

    public async Task<IEnumerable<Tamanio>> ObtenerTamaniosAsync(IDbConnection conexion, IDbTransaction transaction)
    {
        return await conexion.QueryAsync<Tamanio>($"{SelectTamanios} ORDER BY id;", transaction: transaction);
    }

    public async Task<Tamanio?> ObtenerTamanioPorIdAsync(int id)
    {
        using var conexion = NuevaConexion();
        return await conexion.QueryFirstOrDefaultAsync<Tamanio>(
            $"{SelectTamanios} WHERE id = @Id;", new { Id = id });
    }

    public async Task<bool> ActualizarMultiplicadorAsync(int id, decimal multiplicador)
    {
        // El nombre no se actualiza nunca: los items historicos lo guardan
        // y renombrar un tamaño romperia la referencia de esos pedidos.
        const string sql = "UPDATE TAMANIO SET multiplicador = @Multiplicador WHERE id = @Id;";

        using var conexion = NuevaConexion();
        var filas = await conexion.ExecuteAsync(sql, new { Id = id, Multiplicador = multiplicador });
        return filas > 0;
    }
}
