using System.Data;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public abstract class DapperRepo
{
    protected IAdo Ado { get; }

    protected DapperRepo(IAdo ado) => Ado = ado;

    protected IDbConnection NuevaConexion() => Ado.GetDbConnection();
}
