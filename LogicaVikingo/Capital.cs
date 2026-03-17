using System;
using System.Collections.Generic;
using System.Linq;
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
            this.IdLugar = IdLugar;
            this.Nombre = Nombre;
            this.MonedasOro = MonedasOro;
        }
        
        public override int CalcularBotin() => (int)(Defensores * FactorRiqueza);
        
    }
}