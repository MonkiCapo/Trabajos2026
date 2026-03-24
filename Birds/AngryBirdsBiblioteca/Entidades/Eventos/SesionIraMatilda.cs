using System.Linq;
using AngryBirdsBiblioteca.Entidades.Islas;

namespace AngryBirdsBiblioteca.Entidades.Eventos
{
    public class SesionIraMatilda : IEvento
    {
        public void Ejecutar(IslaPajaro isla)
        {
            foreach (var pajaro in isla.Pajaros)
            {
                pajaro.Tranquilizar(5);
            }
        }
    }
}