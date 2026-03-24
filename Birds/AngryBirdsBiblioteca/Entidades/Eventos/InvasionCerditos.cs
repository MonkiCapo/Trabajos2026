using System.Linq;
using AngryBirdsBiblioteca.Entidades.Islas;

namespace AngryBirdsBiblioteca.Entidades.Eventos
{
    public class InvasionCerditos : IEvento
    {
        public int CantidadCerditos { get; private set; }

        public InvasionCerditos(int cantidadCerditos)
        {
            CantidadCerditos = cantidadCerditos;
        }

        public void Ejecutar(IslaPajaro isla)
        {
            int vecesEnojarse = CantidadCerditos / 100;
            for (int i = 0; i < vecesEnojarse; i++)
            {
                foreach (var pajaro in isla.Pajaros)
                {
                    pajaro.Enojarse();
                }
            }
        }
    }
}