using System.Data.Common;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IAdo
{
    DbConnection GetDbConnection();
}