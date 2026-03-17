using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Jarl : ClaseSocial
    {
        public Jarl(int id, string nombre) : base(id, nombre){}
        public override void Ascender(Vikingo usuario)
        {
            usuario.casta = new Karl(1, "asd");

            if (usuario is Soldado soldado)
                soldado.Armas += 10;

            if (usuario is Granjero granjero)
            {
                granjero.Hijos += 2;
                granjero.Hectareas += 2;
            }

        }
    }
}