using System.ComponentModel.DataAnnotations;

namespace MVC.Pizzeria.Models
{
    public class ConfirmacionViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del email no es valido.")]
        [StringLength(150, ErrorMessage = "El email no puede superar 150 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El telefono es obligatorio.")]
        [StringLength(20, ErrorMessage = "El telefono no puede superar 20 caracteres.")]
        [RegularExpression(@"^[\d\s\-\+]+$", ErrorMessage = "El telefono solo puede contener numeros, espacios, guiones y '+'.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La direccion es obligatoria.")]
        [StringLength(200, ErrorMessage = "La direccion no puede superar 200 caracteres.")]
        public string Direccion { get; set; } = string.Empty;

        public CarritoViewModel Carrito { get; set; } = new();
    }
}
