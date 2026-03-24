using System.Collections.Generic;
using System.Linq;
using AngryBirdsBiblioteca.Entidades.Obstaculos;

namespace AngryBirdsBiblioteca.Entidades.Islas
{
    public class IslaCerdito
    {
        public List<IObstaculo> Obstaculos { get; set; }

        public IslaCerdito()
        {
            Obstaculos = new List<IObstaculo>();
        }

        public bool HuevosRecuperados()
        {
            return !Obstaculos.Any();
        }
    }
}