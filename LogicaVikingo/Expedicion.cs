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
        public List<Lugar> lugares;

        public Expedicion(int IdExpedicion)
        {
            this.IdExpedicion = IdExpedicion;
            this.vikingos = new List<Vikingo>();
            this.lugares = new List<Lugar>();
        }

        private void TestEntrada(Vikingo vikingo)
        {
            if (vikingo is Soldado soldado)
            {
                bool jarlConArmas = vikingo.casta is Jarl && soldado.Armas > 0;

                if (soldado.Armas <= 0 || soldado.VidasCobradas <= 20 || jarlConArmas)
                    throw new InvalidOperationException("Soldado no apto");
            }
            else if (vikingo is Granjero granjero)
            {
                if (granjero.Hectareas < granjero.Hijos * 2)
                    throw new InvalidOperationException("Granjero no apto");
            }
        }

        public void SubirVikingo(Vikingo vikingo) 
        {
            TestEntrada(vikingo);
            vikingos.Add(vikingo);
            Console.WriteLine("Vikingo ha subido exitosamente");
        }

        public void InvadirLugar()
        {

        }
    }
}