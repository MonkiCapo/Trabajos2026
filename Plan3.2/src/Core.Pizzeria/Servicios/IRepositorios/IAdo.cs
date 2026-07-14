using System.Data;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IAdo
{
    IDbConnection GetDbConnection();
}
