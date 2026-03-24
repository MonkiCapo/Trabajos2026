using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class Aldea : Lugar
    {
        public int Crucifijos;

        public Aldea (int Crucifijos, int IdLugar, string Nombre, int MonedasOro) : base (IdLugar, Nombre, MonedasOro)
        {
            this.Crucifijos = Crucifijos;
            this.IdLugar = IdLugar;
            this.Nombre = Nombre;
            this.MonedasOro = MonedasOro;
        }

        public override int CalcularBotin()
        {
            return Crucifijos;
        }
        public override bool ValeLaPena(int cantidadVikingos) => CalcularBotin() < 15;
        
    }
}