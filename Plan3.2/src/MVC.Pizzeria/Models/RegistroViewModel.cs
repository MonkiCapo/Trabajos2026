using System.ComponentModel.DataAnnotations;

namespace MVC.Pizzeria.Models;

// Reglas alineadas con RegistroRequestValidator de la API: lo que el MVC
// deja pasar, la API tambien lo acepta (una sola fuente de validacion).
public class RegistroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un email valido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El telefono no puede superar 20 caracteres.")]
    [RegularExpression(@"^[\d\s\-\+]+$", ErrorMessage = "El telefono solo puede contener numeros, espacios, guiones y '+'.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "La direccion es obligatoria.")]
    [StringLength(200, ErrorMessage = "La direccion no puede superar 200 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W]).*$",
        ErrorMessage = "La contraseña debe tener al menos una mayuscula, una minuscula, un numero y un simbolo.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
