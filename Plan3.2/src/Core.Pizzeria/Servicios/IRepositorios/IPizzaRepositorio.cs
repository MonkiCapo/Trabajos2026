using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios.IRepositorios;

public interface IPizzaRepositorio
{
    IEnumerable<Pizza> ObtenerPizzas();
    Pizza ObtenerPizzaPorNombre(string nombre);
}
