using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Thrall : ClaseSocial
    {
        public Thrall (int IdClaseSocial, string Nombre) : base(IdClaseSocial, Nombre)
        {
            this.IdClaseSocial = IdClaseSocial;
            this.Nombre = Nombre;
        }
        public override void Ascender(Vikingo vikingo) => throw new InvalidOperationException("Los Thrall no pueden ascender");
    }
}