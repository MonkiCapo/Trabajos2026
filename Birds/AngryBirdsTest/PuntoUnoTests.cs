using AngryBirdsBiblioteca.Entidades.Pajaros;
using AngryBirdsBiblioteca.Entidades.Abstract;
using Xunit;

namespace AngryBirdsTest;

public class BirdsTests
{
    [Fact]
    public void PajarosComunes_TienenFuerzaDobleDeIra()
    {
        var pajaro = new Pajaros(10);
        Assert.Equal(20, pajaro.Fuerza);
        pajaro.Enojarse();
        Assert.Equal(40, pajaro.Fuerza);
    }

    [Fact]
    public void Red_FuerzaDependeDeIraYEnojos()
    {
        var red = new Red(10);
        Assert.Equal(0, red.Fuerza); // 10 * 10 * 0
        red.Enojarse();
        Assert.Equal(200, red.Fuerza); // Ira=20, Enojos=1 -> 20 * 10 * 1 = 200
    }

    [Fact]
    public void Bomb_FuerzaTieneTope()
    {
        var bomb = new Bomb(5000);
        Assert.Equal(9000, bomb.Fuerza); // 5000 * 2 = 10000, cap is 9000
    }

    [Fact]
    public void Chuck_FuerzaDependeDeVelocidad()
    {
        var chuck = new Chuck(100);
        Assert.Equal(250, chuck.Fuerza); // 150 + 5 * (100 - 80) = 150 + 100 = 250
        chuck.Enojarse();
        Assert.Equal(200, chuck.Velocidad);
        Assert.Equal(750, chuck.Fuerza); // 150 + 5 * (200 - 80) = 150 + 600 = 750
    }

    [Fact]
    public void Matilda_FuerzaIncluyeHuevos()
    {
        var matilda = new Matilda(10);
        Assert.Equal(20, matilda.Fuerza);
        matilda.Enojarse();
        Assert.Equal(42, matilda.Fuerza); // Ira = 20, 20*2=40. Huevo = 2. Total 42.
    }

    [Fact]
    public void Terence_FuerzaConMultiplicador()
    {
        var terence = new Terence(10, 2);
        Assert.Equal(0, terence.Fuerza); // 10 * 0 * 2 = 0
        terence.Enojarse();
        Assert.Equal(40, terence.Fuerza); // 20 * 1 * 2 = 40
    }
}
