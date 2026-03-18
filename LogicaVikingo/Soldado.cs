using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
   public sealed class Soldado : Vikingo
    {
        public int Armas { get; set; }
        public int VidasCobradas { get; set; }

        public Soldado (int Armas, int VidasCobradas, ClaseSocial casta, string Nombre, int IdVikingo) : base (IdVikingo , Nombre, casta)
        {
            this.Armas = Armas;
            this.VidasCobradas = VidasCobradas;
        }
        public void AgregarArmas(int armas) => Armas += armas;
        public void CobrarVida(int vidas) => VidasCobradas += vidas;

        public override void AscenderCasta()
        {
            casta = casta.Ascender();
            if (casta is Karl castaMedia)
                Armas += 10;
        }
        public override void EsProductivo()
        {
            if (casta is Jarl clasebaja && Armas > 0)
                throw new InvalidOperationException("El soldado de casta baja no puede subir porque tiene armas.");

            if(soldado.Armas <= 0 || soldado.VidasCobradas <= 20)
                throw new InvalidOperationException("El soldado no es productivo.");
            
            Console.WriteLine("El soldado es productivo");
        }       
    }
}