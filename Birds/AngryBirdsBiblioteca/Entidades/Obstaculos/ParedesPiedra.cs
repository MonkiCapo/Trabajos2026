namespace AngryBirdsBiblioteca.Entidades.Obstaculos
{
    public class ParedesPiedra : IObstaculo
    {
        public int Ancho { get; private set; }
        public int Resistencia => 50 * Ancho;

        public ParedesPiedra(int ancho)
        {
            Ancho = ancho;
        }
    }
}