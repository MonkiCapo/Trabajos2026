using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public sealed class Granjero : Vikingo
    {
        public int Hectareas { get; set; }
        public int Hijos { get; set; }

        public Granjero( int Hectareas, int Hijos, ClaseSocial casta, string Nombre, int IdVikingo) : base ( IdVikingo, Nombre, casta)
        {
            this.Hectareas = Hectareas;
            this.Hijos = Hijos;
            this.casta = casta;
        }
        public void AgregarHijos(int hijos) => Hijos += hijos;
        public void AgregarHectareas(int hectareas) => Hectareas += hectareas;
    }
}