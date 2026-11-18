using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class IngredienteRepositorio : DapperRepo, IIngredienteRepositorio
{
    public IngredienteRepositorio(IAdo ado) : base(ado) { }

    public async Task<IEnumerable<Ingrediente>> ObtenerIngredientesAsync()
    {
        var sql = "SELECT id, nombre FROM INGREDIENTE ORDER BY nombre;";

        using var conexion = NuevaConexion();
        return await conexion.QueryAsync<Ingrediente>(sql);
    }

    public async Task<IEnumerable<Ingrediente>> ObtenerPorNombresAsync(IEnumerable<string> nombres, IDbConnection conexion, IDbTransaction transaction)
    {
        var sql = "SELECT id, nombre FROM INGREDIENTE WHERE nombre IN @Nombres;";

        return await conexion.QueryAsync<Ingrediente>(sql, new { Nombres = nombres }, transaction);
    }
}
