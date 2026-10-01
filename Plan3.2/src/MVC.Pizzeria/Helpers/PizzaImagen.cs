namespace MVC.Pizzeria.Helpers
{
    public static class PizzaImagen
    {
        public static string UrlDe(int pizzaId) => $"/images/pizza-{pizzaId}.jpg";
    }
}
