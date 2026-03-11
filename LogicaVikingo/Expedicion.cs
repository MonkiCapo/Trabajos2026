using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class Expedicion
    {
        public int IdExpedicion;
        public List<Vikingo> vikingos;
        public List<Lugar> Lugares;

        public void TestEntrada(Vikingo vikingo)
        {
            if( vikingo.GetType() is typeof(Soldado))
            {
                (int Armas, int VidasCobradas ) = vikingo.RevisarProductividad();
                
                bool JarlPoseeArmas = vikingo.casta is Jarl && Armas > 0;

                if( Armas <= 0 && VidasCobradas < 20 && JarlPoseeArmas)
                    throw new InvalidOperationException("El vikingo no es lo suficientemente productivo");
            }
            if (vikingo.GetType() is typeof(Granjero))
            {
                if (Hectareas > Hijos * 2)
                    throw new InvalidOperationException("El vikingo no es lo suficientemente productivo");
            }
        }
        private void SubirVikingo(Vikingo vikingo) 
        {
            TestEntrada(vikingo);
            vikingos.Add(vikingo);
        }
    }
}