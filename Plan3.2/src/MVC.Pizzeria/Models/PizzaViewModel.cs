namespace MVC.Pizzeria.Models
{
    public class PizzaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tamanio { get; set; }
        public decimal Precio { get; set; }
        public List<string> Ingredientes { get; set; } = new();
        public string ImagenUrl { get; set; } = string.Empty;
    }
}