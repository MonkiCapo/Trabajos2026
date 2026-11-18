using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public abstract class DapperRepo
{
    private readonly IAdo _ado;

    protected DapperRepo(IAdo ado)
    {
        _ado = ado;
    }

    protected async Task<DbConnection> AbrirConexionAsync(CancellationToken ct)
    {
        var conexion = _ado.GetDbConnection();
        await conexion.OpenAsync(ct);
        return conexion;
    }
}