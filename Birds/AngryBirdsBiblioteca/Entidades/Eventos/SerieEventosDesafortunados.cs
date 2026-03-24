using System.Collections.Generic;
using AngryBirdsBiblioteca.Entidades.Islas;

namespace AngryBirdsBiblioteca.Entidades.Eventos
{
    public class SerieEventosDesafortunados : IEvento
    {
        public List<IEvento> Eventos { get; private set; }

        public SerieEventosDesafortunados(List<IEvento> eventos)
        {
            Eventos = eventos;
        }

        public void Ejecutar(IslaPajaro isla)
        {
            foreach (var evento in Eventos)
            {
                evento.Ejecutar(isla);
            }
        }
    }
}
