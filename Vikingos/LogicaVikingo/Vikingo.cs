using System.Collections;
using System.Runtime.CompilerServices;

namespace LogicaVikingo;

public abstract class Vikingo
{
    public int IdVikingo;
    public string Nombre = string.Empty;
    public ClaseSocial casta;

    public int OroEnBolsa { get; private set; }

    public Vikingo ( int IdVikingo, string Nombre, ClaseSocial casta)
    {
        this.IdVikingo = IdVikingo;
        this.Nombre = Nombre;
        this.casta = casta;
    }
    
    public abstract void EsProductivo();

    public abstract void AscenderCasta();
    public void LlevarseBotin(int oro)
    {
        OroEnBolsa += oro;
    }

}
