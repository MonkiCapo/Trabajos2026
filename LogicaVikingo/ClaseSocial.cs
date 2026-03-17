using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public abstract class ClaseSocial
    {
        public int IdClaseSocial;
        public string Nombre = string.Empty;

        public ClaseSocial(int IdClaseSocial, string Nombre)
        {
            this.IdClaseSocial = IdClaseSocial;
            this.Nombre = Nombre;
        }
        public abstract void Ascender(Vikingo usuario);
    }
}