using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Karl : ClaseSocial
    {
        public override ClaseSocial Ascender (this Vikingo usuario) => usuario.casta = new Thrall();
    }
}