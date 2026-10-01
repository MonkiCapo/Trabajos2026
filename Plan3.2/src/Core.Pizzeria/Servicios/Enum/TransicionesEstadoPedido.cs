namespace Core.Pizzeria.Servicios.Enum;

public static class TransicionesEstadoPedido
{
    public static bool EsValida(EstadoPedido actual, EstadoPedido siguiente)
    {
        return (actual, siguiente) switch
        {
            (EstadoPedido.EsperaConfirmacion, EstadoPedido.EnPreparacion) => true,
            (EstadoPedido.EsperaConfirmacion, EstadoPedido.Cancelado) => true,
            (EstadoPedido.EnPreparacion, EstadoPedido.EnViaje) => true,
            (EstadoPedido.EnPreparacion, EstadoPedido.Cancelado) => true,
            (EstadoPedido.EnViaje, EstadoPedido.Entregado) => true,
            (EstadoPedido.EnViaje, EstadoPedido.Cancelado) => true,
            _ => false
        };
    }


    public static IEnumerable<EstadoPedido> SiguientesValidos(EstadoPedido actual)
    {
        // Se usa System.Enum porque este namespace se llama Enum y lo pisa.
        return System.Enum.GetValues<EstadoPedido>().Where(siguiente => EsValida(actual, siguiente));
    }

    public static string Describir(EstadoPedido actual)
    {
        var validos = SiguientesValidos(actual).ToList();

        return validos.Count == 0
            ? $"{actual} es un estado final, no admite mas transiciones."
            : $"Desde {actual} se puede pasar a: {string.Join(" o ", validos)}.";
    }
}
