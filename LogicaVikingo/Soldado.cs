using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class Soldado : Vikingo
    {
        public int Armas;
        public int VidasCobradas;

        public override (int, int) RevisarProductividad() => ( Armas, VidasCobradas);
    }
}