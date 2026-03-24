using System.Collections.Generic;
using AngryBirdsBiblioteca.Entidades.Islas;
using AngryBirdsBiblioteca.Entidades.Obstaculos;
using AngryBirdsBiblioteca.Entidades.Pajaros;
using Xunit;

namespace AngryBirdsTest;

public class GuerraPorcinaTests
{
    [Fact]
    public void PajaroFuerteDerribaObstaculo()
    {
        var islaPajaro = new IslaPajaro();
        var chuck = new Chuck(100); // Fuerza = 250
        islaPajaro.Pajaros.Add(chuck);

        var islaCerdito = new IslaCerdito();
        islaCerdito.Obstaculos.Add(new ParedesMadera(5)); // Resistencia 25 * 5 = 125

        islaPajaro.Atacar(islaCerdito);

        Assert.True(islaCerdito.HuevosRecuperados()); // Obstáculo destruido
    }

    [Fact]
    public void PajaroDebilNoDerribaObstaculo()
    {
        var islaPajaro = new IslaPajaro();
        var red = new Red(10); // Fuerza = 0
        islaPajaro.Pajaros.Add(red);

        var islaCerdito = new IslaCerdito();
        islaCerdito.Obstaculos.Add(new ParedesPiedra(2)); // Resistencia 100

        islaPajaro.Atacar(islaCerdito);

        Assert.False(islaCerdito.HuevosRecuperados()); // Obstáculo sigue en pie
    }
}
