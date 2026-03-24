using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Abstract;

namespace AngryBirdsBiblioteca.Entidades.Pajaros
{
    public class Chuck : Pajaro
    {
        public int Velocidad { get; private set; }
        public override int Fuerza => CalcularFuerza();

        public Chuck(int velocidad) : base(velocidad)
        {
            Velocidad = velocidad;
            Ira = velocidad;
        }

        public override void Enojarse()
        {
            Velocidad *= 2;
        }

        public override void Tranquilizar(int cantidad)
        {
            // A Chuck nada lo tranquiliza
        }

        private int CalcularFuerza()
        {   
            if (Velocidad <= 80)
            {
                return 150;
            }
            return 150 + 5 * (Velocidad - 80);
        }
    }
}