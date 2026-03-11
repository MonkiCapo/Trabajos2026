using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class Granjero : Vikingo
    {
        public int Hectareas;
        public int Hijos;

        public override (int, int) RevisarProductividad() => ( Hectareas, Hijos);
    }
}