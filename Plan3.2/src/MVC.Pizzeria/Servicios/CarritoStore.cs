using System.Text.Json;
using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios
{
    public class CarritoStore : ICarritoStore
    {
        /// <summary>
        /// Tope por pizza. Coincide con el limite que pone la API en
        /// CheckoutRequestValidator, asi el carrito nunca genera un 400.
        /// </summary>
        public const int CANTIDAD_MAXIMA = 99;

        private const string CLAVE = "Carrito";

        private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CarritoStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// La sesion se resuelve aca y no en el constructor a proposito:
        /// ISession no se puede inyectar, ASP.NET Core no lo registra en el
        /// contenedor de DI. Lo unico disponible es HttpContext.Session, que
        /// arma el middleware app.UseSession() antes de ejecutar los endpoints.
        /// </summary>
        private ISession Session =>
            _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException(
                "No hay sesion disponible en el contexto actual. Revisa que app.UseSession() "
                + "esté en el pipeline y que AddSession() esté registrado.");

        public CarritoViewModel Obtener()
        {
            var json = Session.GetString(CLAVE);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new CarritoViewModel();
            }

            try
            {
                return JsonSerializer.Deserialize<CarritoViewModel>(json, OpcionesJson) ?? new CarritoViewModel();
            }
            catch (JsonException)
            {
                // Si la cookie se corrupto, se descarta y se sigue como si
                // el carrito estuviera vacio. Es preferible a tirar 500.
                Session.Remove(CLAVE);
                return new CarritoViewModel();
            }
        }

        public void Guardar(CarritoViewModel carrito)
        {
            Session.SetString(CLAVE, JsonSerializer.Serialize(carrito, OpcionesJson));
        }

        public void Agregar(ItemCarritoViewModel item, int cantidad)
        {
            var carrito = Obtener();
            var linea = carrito.Items.FirstOrDefault(i => i.PizzaId == item.PizzaId);

            if (linea is null)
            {
                carrito.Items.Add(new ItemCarritoViewModel
                {
                    PizzaId = item.PizzaId,
                    Nombre = item.Nombre,
                    Tamanio = item.Tamanio,
                    ImagenUrl = item.ImagenUrl,
                    Precio = item.Precio,
                    Cantidad = Math.Clamp(cantidad, 1, CANTIDAD_MAXIMA)
                });
            }
            else
            {
                linea.Cantidad = Math.Clamp(linea.Cantidad + cantidad, 1, CANTIDAD_MAXIMA);
            }

            Guardar(carrito);
        }

        public void Actualizar(int pizzaId, int cantidad)
        {
            var carrito = Obtener();

            if (cantidad <= 0)
            {
                carrito.Items.RemoveAll(i => i.PizzaId == pizzaId);
            }
            else
            {
                var linea = carrito.Items.FirstOrDefault(i => i.PizzaId == pizzaId);
                if (linea is not null)
                {
                    linea.Cantidad = Math.Min(cantidad, CANTIDAD_MAXIMA);
                }
            }

            Guardar(carrito);
        }

        public void Quitar(int pizzaId)
        {
            var carrito = Obtener();
            carrito.Items.RemoveAll(i => i.PizzaId == pizzaId);
            Guardar(carrito);
        }

        public void Vaciar()
        {
            Session.Remove(CLAVE);
        }
    }
}
