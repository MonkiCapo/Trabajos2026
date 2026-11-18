using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MVC.Pizzeria.Models;

public class PizzaAdminViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tamaño es obligatorio.")]
    [StringLength(50, ErrorMessage = "El tamaño no puede superar 50 caracteres.")]
    public string Tamanio { get; set; } = string.Empty;

    // Sobrecarga numerica (sin strings): la version con typeof(decimal)
    // parsea con la cultura del sistema y explota en es-AR ("0,01").
    [Range(0.01, 99999999.99, ErrorMessage = "El precio debe ser mayor a 0 y tener como máximo 2 decimales.")]
    public decimal Precio { get; set; }

    public List<string> IngredientesSeleccionados { get; set; } = new();
    public List<SelectListItem> IngredientesDisponibles { get; set; } = new();
    public string ImagenActual { get; set; } = string.Empty;

    [Display(Name = "Imagen")]
    public IFormFile? Imagen { get; set; }
}
