namespace MVC.Pizzeria.Helpers
{
    public static class EstadoPedidoUI
    {
        public static string BadgeDe(string? estado) => estado switch
        {
            "EsperaConfirmacion" => "bg-secondary",
            "EnPreparacion" => "bg-warning text-dark",
            "EnViaje" => "bg-info text-dark",
            "Entregado" => "bg-success",
            "Cancelado" => "bg-danger",
            _ => "bg-secondary"
        };

        public static string NombreDe(string? estado) => estado switch
        {
            "EsperaConfirmacion" => "Pedido recibido",
            "EnPreparacion" => "En preparación",
            "EnViaje" => "En camino",
            "Entregado" => "Entregado",
            "Cancelado" => "Cancelado",
            _ => string.IsNullOrWhiteSpace(estado) ? "Sin estado" : estado
        };

        public static int NumeroDe(string? estado) => estado switch
        {
            "EsperaConfirmacion" => 1,
            "EnPreparacion" => 2,
            "EnViaje" => 3,
            "Entregado" => 4,
            "Cancelado" => 5,
            _ => 0
        };

        public static readonly string[] Recorrido =
        {
            "EsperaConfirmacion",
            "EnPreparacion",
            "EnViaje",
            "Entregado"
        };
    }
}