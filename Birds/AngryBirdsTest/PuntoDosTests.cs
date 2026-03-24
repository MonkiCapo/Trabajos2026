using System.Collections.Generic;
using AngryBirdsBiblioteca.Entidades.Islas;
using AngryBirdsBiblioteca.Entidades.Eventos;
using AngryBirdsBiblioteca.Entidades.Pajaros;
using AngryBirdsBiblioteca.Entidades.Abstract;
using Xunit;

namespace AngryBirdsTest;

public class EventosTests
{
    [Fact]
    public void SesionIraMatilda_TranquilizaTodosMenosChuck()
    {
        var isla = new IslaPajaro();
        var red = new Red(10);
        var chuck = new Chuck(100);
        isla.Pajaros.Add(red);
        isla.Pajaros.Add(chuck);

        isla.OcurrirEvento(new SesionIraMatilda());

        Assert.Equal(5, red.Ira);
        Assert.Equal(100, chuck.Ira); // Chuck ignores Tranquilizar
    }

    [Fact]
    public void InvasionCerditos_EnojaPajarosSegunCantidad()
    {
        var isla = new IslaPajaro();
        var red = new Red(10);
        isla.Pajaros.Add(red);

        isla.OcurrirEvento(new InvasionCerditos(250)); // Enoja 2 veces

        Assert.Equal(40, red.Ira); // 10 -> 20 -> 40
        Assert.Equal(2, red.CantidadDeEnojos);
    }
}
