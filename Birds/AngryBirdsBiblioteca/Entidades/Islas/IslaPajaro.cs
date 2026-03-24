using System;
using System.Collections.Generic;
using System.Linq;
using AngryBirdsBiblioteca.Entidades.Abstract;
using AngryBirdsBiblioteca.Entidades.Eventos;

namespace AngryBirdsBiblioteca.Entidades.Islas
{
    public class IslaPajaro
    {
        public List<Pajaro> Pajaros { get; set; }

        public IslaPajaro()
        {
            Pajaros = new List<Pajaro>();
        }

        public IEnumerable<Pajaro> PajarosFuertes => Pajaros.Where(p => p.EsFuerte);

        public int Fuerza => PajarosFuertes.Sum(p => p.Fuerza);

        public void OcurrirEvento(IEvento evento)
        {
            evento.Ejecutar(this);
        }

        public void Atacar(IslaCerdito islaCerdito)
        {
            foreach (var pajaro in Pajaros)
            {
                if (!islaCerdito.Obstaculos.Any())
                    break;

                var obstaculoActual = islaCerdito.Obstaculos.First();
                if (pajaro.PuedeDerribar(obstaculoActual))
                {
                    islaCerdito.Obstaculos.RemoveAt(0);
                }
            }
        }
    }
}