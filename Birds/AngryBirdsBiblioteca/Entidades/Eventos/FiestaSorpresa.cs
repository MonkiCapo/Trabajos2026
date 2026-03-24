using System.Collections.Generic;
using AngryBirdsBiblioteca.Entidades.Abstract;
using AngryBirdsBiblioteca.Entidades.Islas;

namespace AngryBirdsBiblioteca.Entidades.Eventos
{
    public class FiestaSorpresa : IEvento
    {
        public List<Pajaro> Homenajeados { get; private set; }

        public FiestaSorpresa(List<Pajaro> homenajeados)
        {
            Homenajeados = homenajeados;
        }

        public void Ejecutar(IslaPajaro isla)
        {
            foreach (var pajaro in Homenajeados)
            {
                if (isla.Pajaros.Contains(pajaro))
                {
                    pajaro.Enojarse();
                }
            }
        }
    }
}