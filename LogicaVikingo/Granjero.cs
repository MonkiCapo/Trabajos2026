using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Granjero : Vikingo
    {
        protected int Hectareas;
        protected int Hijos;

        public void AgregarHijos(int hijos) => Hijos += hijos;

        public void AgregarHectareas(int hectareas) => Hectareas += hectareas; 

        public override (int, int) RevisarProductividad() => (Hectareas, Hijos);
    }
}