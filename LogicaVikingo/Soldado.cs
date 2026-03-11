using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Soldado : Vikingo
    {
        protected int Armas;
        protected int VidasCobradas;

        public void AgregarArmas(int armas) => Armas += armas;
        public void CobrarVida(int vidas) => VidasCobradas += vidas;
        
        public override (int, int) RevisarProductividad() => ( Armas, VidasCobradas);
    }
}