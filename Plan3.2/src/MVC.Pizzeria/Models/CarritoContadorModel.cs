namespace MVC.Pizzeria.Models
{
    /// <summary>
    /// Datos que necesita el badge del navbar. Vive en Models para que el
    /// @model de la vista del ViewComponent lo resuelva solo, como el resto.
    /// </summary>
    public class CarritoContadorModel
    {
        public int CantidadTotal { get; set; }
        public bool HayItems { get; set; }
    }
}
