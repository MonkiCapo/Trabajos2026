using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Thrall : ClaseSocial
    {
        public override ClaseSocial Ascender() => throw new InvalidOperationException("Los Thrall no pueden ascender");
    }
}