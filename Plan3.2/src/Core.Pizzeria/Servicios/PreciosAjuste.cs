namespace Core.Pizzeria.Servicios;

// Reglas de precio de "Arma tu pizza" (Fase 8): quitar no cambia el precio
// y cada extra suma un recargo fijo. Las comparten la API (autoridad) y el
// MVC (para mostrar el recargo en la UI).
public static class PreciosAjuste
{
    public const decimal RecargoExtra = 0.50m;
    public const int MaxExtras = 5;
}
