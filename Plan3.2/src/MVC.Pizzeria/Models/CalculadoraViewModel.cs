using System.ComponentModel.DataAnnotations;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;

namespace MVC.Pizzeria.Models;

// Formulario de la calculadora de hambre: los datos que ingresa el usuario
// (personas, ninos, hambre y pizza elegida) mas todo lo que la vista
// necesita para mostrar la sugerencia (catalogo y tamanios reales de la API).
public class CalculadoraViewModel
{
    [Range(1, 20, ErrorMessage = "Indicá entre 1 y 20 personas.")]
    public int Personas { get; set; } = 4;

    [Range(0, 20, ErrorMessage = "Los niños van de 0 a 20.")]
    public int Ninos { get; set; }

    public NivelHambre NivelHambre { get; set; } = NivelHambre.Normal;

    public int PizzaId { get; set; }

    public List<PizzaViewModel> Pizzas { get; set; } = new();
    public List<Tamanio> Tamanios { get; set; } = new();

    public List<SugerenciaTamanio> Sugerencia { get; set; } = new();
    public int PorcionesNecesarias { get; set; }
    public decimal PrecioTotal { get; set; }
}
