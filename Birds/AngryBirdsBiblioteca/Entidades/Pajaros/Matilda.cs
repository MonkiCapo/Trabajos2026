using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Abstract;

namespace AngryBirdsBiblioteca.Entidades.Pajaros
{
    public class Matilda : Pajaro
    {
        public List<Huevo> Huevos { get; private set; }
        public override int Fuerza => (Ira * 2) + Huevos.Sum(h => h.Fuerza);

        public Matilda(int ira) : base(ira)
        {
            Huevos = new List<Huevo>();
        }

        public override void Enojarse()
        {
            base.Enojarse();
            Huevos.Add(new Huevo(2)); // Pone un huevo de 2 kilos al enfadarse
        }
    }

    public class Huevo
    {
        public int Peso { get; private set; }
        public int Fuerza => Peso;

        public Huevo(int peso)
        {
            Peso = peso;
        }
    }
}