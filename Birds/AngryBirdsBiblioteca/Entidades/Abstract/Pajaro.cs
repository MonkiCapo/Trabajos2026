using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Obstaculos;

namespace AngryBirdsBiblioteca.Entidades.Abstract
{
    public abstract class Pajaro
    {
        public int Ira {get; set;}
        public abstract int Fuerza {get;}

        public bool EsFuerte => Fuerza > 50;

        public Pajaro(int ira)
        {
            Ira = ira;
        }

        public virtual void Enojarse()
        {
            Ira *= 2;
        }

        public virtual void Tranquilizar(int cantidad)
        {
            Ira = Math.Max(0, Ira - cantidad);
        }

        public virtual bool PuedeDerribar(IObstaculo obstaculo)
        {
            return Fuerza > obstaculo.Resistencia;
        }
    }
}