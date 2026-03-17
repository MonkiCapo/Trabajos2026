using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public abstract class Lugar
    {
        public int IdLugar;
        public string Nombre = string.Empty;
        public int MonedasOro;
        
        public Lugar (int IdLugar, string Nombre, int MonedasOro)
        {
            this.IdLugar = IdLugar;
            this.Nombre = Nombre;
            this.MonedasOro = MonedasOro;
        }
        public abstract int CalcularBotin();
    }
}