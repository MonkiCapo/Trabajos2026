namespace MVC.Pizzeria.Helpers
{
    /// <summary>
    /// Presentacion de los estados del pedido en las vistas.
    ///
    /// La API devuelve el estado como el nombre del enum (por ejemplo
    /// "EsperaConfirmacion"). Estas funciones son el unico lugar donde se
    /// traduce eso a color y a texto legible, asi no hay que repetir el switch
    /// en cada .cshtml.
    /// </summary>
    public static class EstadoPedidoUI
    {
        /// <summary>Clase de Bootstrap para el badge del estado.</summary>
        public static string BadgeDe(string? estado) => estado switch
        {
            "EsperaConfirmacion" => "bg-secondary",
            "EnPreparacion" => "bg-warning text-dark",
            "EnViaje" => "bg-info text-dark",
            "Entregado" => "bg-success",
            "Cancelado" => "bg-danger",
            _ => "bg-secondary"
        };

        /// <summary>Texto que ve el cliente, en lugar del nombre del enum.</summary>
        public static string NombreDe(string? estado) => estado switch
        {
            "EsperaConfirmacion" => "Pedido recibido",
            "EnPreparacion" => "En preparación",
            "EnViaje" => "En camino",
            "Entregado" => "Entregado",
            "Cancelado" => "Cancelado",
            _ => string.IsNullOrWhiteSpace(estado) ? "Sin estado" : estado
        };

        /// <summary>
        /// Posicion del estado en el recorrido 1..4. Sirve para saber hasta
        /// donde llego el pedido en la barra de avance. Cancelado devuelve 5
        /// porque esta fuera del recorrido normal.
        /// </summary>
        public static int NumeroDe(string? estado) => estado switch
        {
            "EsperaConfirmacion" => 1,
            "EnPreparacion" => 2,
            "EnViaje" => 3,
            "Entregado" => 4,
            "Cancelado" => 5,
            _ => 0
        };

        /// <summary>Los cuatro estados del recorrido normal, en orden.</summary>
        public static readonly string[] Recorrido =
        {
            "EsperaConfirmacion",
            "EnPreparacion",
            "EnViaje",
            "Entregado"
        };
    }
}