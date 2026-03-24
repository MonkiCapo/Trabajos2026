using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Abstract;

namespace AngryBirdsBiblioteca.Entidades.Pajaros
{
    public class Red : Pajaro
    {
        public int CantidadDeEnojos {get; private set;}
        public override int Fuerza => Ira * 10 * CantidadDeEnojos;

        public Red (int ira) : base(ira)
        {
            CantidadDeEnojos = 0;
        }

        public override void Enojarse()
        {
            base.Enojarse();
            CantidadDeEnojos++;
        }
    }
}