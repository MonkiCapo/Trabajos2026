using Core.Pizzeria.Servicios;

namespace MVC.Pizzeria.Models;

// Formulario de "Arma tu pizza": los ingredientes de la pizza van tildados
// por defecto (destildar = quitar) y los del catalogo que la pizza no trae
// se ofrecen como extras con recargo.
public class PersonalizarViewModel
{
    public int PizzaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tamanio { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;

    public decimal Precio { get; set; }
    public int Cantidad { get; set; } = 1;

    public List<string> IngredientesPizza { get; set; } = new();
    public List<string> ExtrasDisponibles { get; set; } = new();

    public List<string> Incluir { get; set; } = new();
    public List<string> Extras { get; set; } = new();

    public decimal RecargoExtra => PreciosAjuste.RecargoExtra;
    public int MaxExtras => PreciosAjuste.MaxExtras;
}
