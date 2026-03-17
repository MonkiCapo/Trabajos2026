using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class AldeaAmurallada : Lugar
    {
        public int Defensores;

        public AldeaAmurallada (int Defensores, int IdLugar, string Nombre, int MonedasOro) : base (IdLugar, Nombre, MonedasOro)
        {
            this.Defensores = Defensores;
            this.IdLugar = IdLugar;
            this.Nombre = Nombre;
            this.MonedasOro = MonedasOro;
        }
        public override int CalcularBotin() => 1;
    }
}