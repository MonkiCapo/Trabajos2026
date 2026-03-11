namespace LogicaVikingo;

public abstract class Vikingo
{
    public int IdVikingo;
    public string Nombre = string.Empty;

    public abstract (int, int) RevisarProductividad();

}
