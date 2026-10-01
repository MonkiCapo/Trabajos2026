namespace MVC.Pizzeria.Models
{
    public class ItemCarritoViewModel
    {
        public int PizzaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tamanio { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;

        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal => Precio * Cantidad;
    }
}
