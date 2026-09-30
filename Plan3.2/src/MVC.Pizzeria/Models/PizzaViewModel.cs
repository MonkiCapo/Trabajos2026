namespace MVC.Pizzeria.Models
{
    public class PizzaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tamanio { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public List<string> Ingredientes { get; set; } = new();
        public string ImagenUrl { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad que eligio el usuario en el formulario de pedido.
        /// 0 significa "no la quiero todavia".
        /// </summary>
        public int Cantidad { get; set; }

        public decimal Subtotal => Precio * Cantidad;
    }
}
