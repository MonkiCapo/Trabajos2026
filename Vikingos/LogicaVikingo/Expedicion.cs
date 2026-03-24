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
        
        public void SubirVikingo(Vikingo vikingo) 
        {
            vikingo.EsProductivo();
            vikingos.Add(vikingo);
            Console.WriteLine("Vikingo ha subido exitosamente");
        }

        public void InvadirLugar(Lugar lugar)
        {
            if(lugares.Contains(lugar))
                return;

            lugar.ValeLaPena();

        }
        public void ValeLaPena()
        {
            foreach (Lugar lugarAInvadir in lugares)
            {
                if(lugarAInvadir.ValeLaPena(vikingos.Count()))
                    return "La expedición no vale la pena";
            }
        }
    }
}