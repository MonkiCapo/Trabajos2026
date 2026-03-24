namespace AngryBirdsBiblioteca.Entidades.Obstaculos
{
    public class CerditoArmado : IObstaculo
    {
        public IEquipamiento Equipamiento { get; private set; }

        public int Resistencia => Equipamiento != null ? 10 * Equipamiento.Resistencia : 50;

        public CerditoArmado(IEquipamiento equipamiento)
        {
            Equipamiento = equipamiento;
        }
    }
}
