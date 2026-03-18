using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;

namespace LogicaVikingo
{
    public sealed class Karl : ClaseSocial
    {
        public Karl(int IdClaseSocial, string Nombre) : base(IdClaseSocial, Nombre)
        {
            this.IdClaseSocial = IdClaseSocial;
            this.Nombre = Nombre;
        }
        public override ClaseSocial Ascender () => new Thrall();
    }
}
