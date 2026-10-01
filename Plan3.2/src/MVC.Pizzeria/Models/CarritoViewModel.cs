namespace MVC.Pizzeria.Models
{
    public class CarritoViewModel
    {
        public List<ItemCarritoViewModel> Items { get; set; } = new();

        //Cantidad total de pizzas
        public int CantidadTotal => Items.Sum(i => i.Cantidad);

        public bool HayItems => Items.Count > 0;

        public decimal Total => Items.Sum(i => i.Subtotal);
    }
}
