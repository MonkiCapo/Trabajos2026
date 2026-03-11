using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Jarl : ClaseSocial
    {
        public override ClaseSocial Ascender(this Vikingo usuario)
        {
            usuario.casta = new Karl();
            if (usuario.GetType() is typeof(Soldado))
                usuario.Armas += 10;

            if (usuario.GetType() is typeof(Granjero)){
                usuario.Hijos += 2;
                usuario.Hectareas += 2;
            }
        }
    }
}