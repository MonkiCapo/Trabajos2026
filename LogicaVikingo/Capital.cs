using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class Capital : Lugar
    {
        public int Defensores;
        public double FactorRiqueza;

        public Capital (int Defensores, double FactorRiqueza, int IdLugar, string Nombre, int MonedasOro) : base (IdLugar, Nombre, MonedasOro)
        {
            this.Defensores = Defensores;
            this.FactorRiqueza = FactorRiqueza;
        }
        
        public override int CalcularBotin() => (int)(Defensores * FactorRiqueza);

        public override bool ValeLaPena(int vikingos) => vikingos % CalcularBotin() <= 3;
        
    }
}