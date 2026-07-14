using System.Data;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public abstract class DapperRepo
{
    protected IDbConnection Conexion { get; set; }

    protected DapperRepo(IAdo _ado) => Conexion = _ado.GetDbConnection();
}
