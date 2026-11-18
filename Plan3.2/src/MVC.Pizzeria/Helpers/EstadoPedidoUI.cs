using Core.Pizzeria.Servicios.Enum;

namespace MVC.Pizzeria.Helpers;

// Traduccion del estado a UI (badge, texto y orden). El enum EstadoPedido es
// la unica fuente de verdad: las vistas siguen pasando el string que viene
// de la API y aca se lo interpreta contra el enum.
public static class EstadoPedidoUI
{
    public static string BadgeDe(string? estado) =>
        Enum.TryParse<EstadoPedido>(estado, out var valor) ? BadgeDe(valor) : "bg-secondary";

    public static string NombreDe(string? estado) =>
        Enum.TryParse<EstadoPedido>(estado, out var valor)
            ? NombreDe(valor)
            : string.IsNullOrWhiteSpace(estado) ? "Sin estado" : estado;

    public static int NumeroDe(string? estado) =>
        Enum.TryParse<EstadoPedido>(estado, out var valor) ? NumeroDe(valor) : 0;

    public static string BadgeDe(EstadoPedido estado) => estado switch
    {
        EstadoPedido.EsperaConfirmacion => "bg-secondary",
        EstadoPedido.EnPreparacion => "bg-warning text-dark",
        EstadoPedido.EnViaje => "bg-info text-dark",
        EstadoPedido.Entregado => "bg-success",
        EstadoPedido.Cancelado => "bg-danger",
        _ => "bg-secondary"
    };

    public static string NombreDe(EstadoPedido estado) => estado switch
    {
        EstadoPedido.EsperaConfirmacion => "Pedido recibido",
        EstadoPedido.EnPreparacion => "En preparación",
        EstadoPedido.EnViaje => "En camino",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Cancelado => "Cancelado",
        _ => estado.ToString()
    };

    public static int NumeroDe(EstadoPedido estado) => (int)estado;

    public static readonly string[] Recorrido =
    {
        EstadoPedido.EsperaConfirmacion.ToString(),
        EstadoPedido.EnPreparacion.ToString(),
        EstadoPedido.EnViaje.ToString(),
        EstadoPedido.Entregado.ToString()
    };
}
