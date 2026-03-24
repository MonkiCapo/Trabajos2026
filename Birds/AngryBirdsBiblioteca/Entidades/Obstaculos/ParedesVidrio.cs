namespace AngryBirdsBiblioteca.Entidades.Obstaculos
{
    public class ParedesVidrio : IObstaculo
    {
        public int Ancho { get; private set; }
        public int Resistencia => 10 * Ancho;

        public ParedesVidrio(int ancho)
        {
            Ancho = ancho;
        }
    }
}