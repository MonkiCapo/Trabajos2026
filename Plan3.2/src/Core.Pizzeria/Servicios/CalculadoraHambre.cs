using System.Collections.Generic;
using System.Linq;
using Core.Pizzeria.Entidades;

namespace Core.Pizzeria.Servicios;

public enum NivelHambre
{
    Poco,
    Normal,
    Mucho
}

public class SugerenciaTamanio
{
    public string Tamanio { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

// Reglas de la "calculadora de hambre": a partir de personas, ninos y nivel
// de hambre sugiere cuantas pizzas de cada tamanio pedir. Una pizza Grande
// (multiplicador 1.00) rinde 8 porciones y los demas tamanios rinden
// proporcional a su multiplicador (Mediano 6, Chico 4). Estas reglas viven
// en Core para poder testearlas sin levantar ni el MVC ni la API.
public static class CalculadoraHambre
{
    public const int PorcionesGrande = 8;

    public static int PorcionesPorTamanio(Tamanio tamanio) =>
        Math.Max(1, (int)Math.Round(PorcionesGrande * tamanio.Multiplicador, MidpointRounding.AwayFromZero));

    public static int PorcionesPorPersona(NivelHambre hambre, bool esNino)
    {
        if (esNino)
        {
            return hambre == NivelHambre.Poco ? 1 : 2;
        }

        return hambre switch
        {
            NivelHambre.Poco => 2,
            NivelHambre.Mucho => 4,
            _ => 3
        };
    }

    public static int TotalPorciones(int personas, int ninos, NivelHambre hambre) =>
        personas * PorcionesPorPersona(hambre, esNino: false)
        + ninos * PorcionesPorPersona(hambre, esNino: true);

    // Greedy simple: se llenan tantos tamanios grandes como entren y el
    // resto se cubre con el tamanio mas chico que alcance. Si ninguno
    // alcanza, se suma un grande mas. El resultado viene ordenado de mayor
    // a menor tamanio.
    public static List<SugerenciaTamanio> Sugerir(int personas, int ninos, NivelHambre hambre, IEnumerable<Tamanio> tamanios)
    {
        var porciones = TotalPorciones(personas, ninos, hambre);

        var disponibles = tamanios
            .OrderByDescending(PorcionesPorTamanio)
            .ToList();

        if (porciones <= 0 || disponibles.Count == 0)
        {
            return new List<SugerenciaTamanio>();
        }

        var cantidades = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void Sumar(Tamanio tamanio)
        {
            cantidades[tamanio.Nombre] = cantidades.GetValueOrDefault(tamanio.Nombre) + 1;
        }

        var grande = disponibles[0];
        var porcionesGrande = PorcionesPorTamanio(grande);
        var cantidadGrande = porciones / porcionesGrande;
        for (var indice = 0; indice < cantidadGrande; indice++)
        {
            Sumar(grande);
        }

        var resto = porciones % porcionesGrande;
        if (resto > 0)
        {
            var queAlcanza = disponibles
                .Where(t => PorcionesPorTamanio(t) >= resto)
                .OrderBy(PorcionesPorTamanio)
                .FirstOrDefault();

            Sumar(queAlcanza ?? grande);
        }

        return disponibles
            .Where(t => cantidades.ContainsKey(t.Nombre))
            .Select(t => new SugerenciaTamanio { Tamanio = t.Nombre, Cantidad = cantidades[t.Nombre] })
            .ToList();
    }
}
