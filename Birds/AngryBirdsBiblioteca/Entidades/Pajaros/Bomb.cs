using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryBirdsBiblioteca.Entidades.Abstract;

namespace AngryBirdsBiblioteca.Entidades.Pajaros
{
    public class Bomb : Pajaro
    {
        private const int FuerzaMax = 9000;

        public static int FuerzaMaxActual {get; set;} = FuerzaMax;


        public override int Fuerza
        {
            get
            {
                int fuerza = Ira * 2;
                return Math.Min(fuerza, FuerzaMaxActual);
            }  
        } 

        public Bomb(int ira) : base(ira)
        {
            
        }
    }
}