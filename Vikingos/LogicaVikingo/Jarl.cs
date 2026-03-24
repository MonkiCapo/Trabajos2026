using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Jarl : ClaseSocial
    {
        public Jarl(int id, string nombre) : base(id, nombre){}
        public override ClaseSocial Ascender() => new Karl();
    }
}