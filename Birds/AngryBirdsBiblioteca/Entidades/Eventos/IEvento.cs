using AngryBirdsBiblioteca.Entidades.Islas;

namespace AngryBirdsBiblioteca.Entidades.Eventos
{
    public interface IEvento
    {
        void Ejecutar(IslaPajaro isla);
    }
}
