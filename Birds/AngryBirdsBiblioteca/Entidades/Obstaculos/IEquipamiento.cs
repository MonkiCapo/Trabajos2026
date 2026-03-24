namespace AngryBirdsBiblioteca.Entidades.Obstaculos
{
    public interface IEquipamiento
    {
        int Resistencia { get; }
    }

    public class Casco : IEquipamiento
    {
        public int ResistenciaPropia { get; private set; }
        public int Resistencia => ResistenciaPropia;

        public Casco(int resistenciaPropia)
        {
            ResistenciaPropia = resistenciaPropia;
        }
    }

    public class Escudo : IEquipamiento
    {
        public int ResistenciaPropia { get; private set; }
        public int Resistencia => ResistenciaPropia;

        public Escudo(int resistenciaPropia)
        {
            ResistenciaPropia = resistenciaPropia;
        }
    }
}
