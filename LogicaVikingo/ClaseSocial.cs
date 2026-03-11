using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public abstract class ClaseSocial
    {
        public int IDClaseSocial;
        public string Nombre = string.Empty;
        public abstract ClaseSocial Ascender();
    }
}