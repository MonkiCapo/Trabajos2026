using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicaVikingo
{
    public class AldeaAmurallada : Aldea
    {
        public int Defensores;
        public AldeaAmurallada (int Defensores, int Crucifijos) : base (Crucifijos)
        {
            this.Defensores = Defensores;
        }
        public override int CalcularBotin() => Defensores;

        
    }
}