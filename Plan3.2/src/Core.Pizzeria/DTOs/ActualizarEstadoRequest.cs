using Core.Pizzeria.Servicios.Enum;

namespace Core.Pizzeria.DTOs;

public class ActualizarEstadoRequest
{
    public EstadoPedido Estado { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
