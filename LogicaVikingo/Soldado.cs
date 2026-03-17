using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
   public sealed class Soldado : Vikingo
    {
        public int Armas { get; set; }
        public int VidasCobradas { get; set; }

        public Soldado (int Armas, int VidasCobradas, ClaseSocial casta, string Nombre, int IdVikingo) : base (IdVikingo , Nombre, casta)
        {
            this.Armas = Armas;
            this.VidasCobradas = VidasCobradas;
        }
        public void AgregarArmas(int armas) => Armas += armas;
        public void CobrarVida(int vidas) => VidasCobradas += vidas;
    }
}