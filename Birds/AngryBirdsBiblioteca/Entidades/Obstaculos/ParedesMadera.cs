namespace AngryBirdsBiblioteca.Entidades.Obstaculos
{
    public class ParedesMadera : IObstaculo
    {
        public int Ancho { get; private set; }
        public int Resistencia => 25 * Ancho;

        public ParedesMadera(int ancho)
        {
            Ancho = ancho;
        }
    }
}