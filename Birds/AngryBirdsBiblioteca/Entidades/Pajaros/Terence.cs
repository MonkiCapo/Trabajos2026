using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Abstract;

namespace AngryBirdsBiblioteca.Entidades.Pajaros
{
    public class Terence : Pajaro
    {
        public int CantidadDeEnojos {get; private set;}
        public int Multiplicador {get; private set;}
        public override int Fuerza => Ira * CantidadDeEnojos * Multiplicador;
        public Terence(int ira, int multiplicador = 1) : base(ira)
        {
            CantidadDeEnojos = 0;
            Multiplicador = multiplicador;
        }

        public override void Enojarse()
        {
            base.Enojarse();
            CantidadDeEnojos++;
        }

        public void ColocarMultiplicador(int multiplicador)
        {
            if (Multiplicador <= 0)
                throw new ArgumentException("El multiplicador debe ser mayor a 0");

            Multiplicador = multiplicador;
        }
    }
}