using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Abstract;

namespace AngryBirdsBiblioteca.Entidades.Pajaros
{
    public class Pajaros : Pajaro
    {
        public override int Fuerza => Ira * 2;

        public Pajaros(int ira) : base(ira)
        {   
        }

    }
}