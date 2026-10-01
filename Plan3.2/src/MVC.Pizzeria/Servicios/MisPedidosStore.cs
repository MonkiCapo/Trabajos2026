using System.Text.Json;
using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios
{
    public class MisPedidosStore : IMisPedidosStore
    {
        public const int TOPE = 20;

        private const string CLAVE = "MisPedidos";

        private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

        private readonly IHttpContextAccessor _httpContextAccessor;

        public MisPedidosStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private ISession Session =>
            _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException(
                "No hay sesion disponible en el contexto actual. Revisa que app.UseSession() esté en el pipeline y que AddSession() esté registrado.");

        public IReadOnlyList<PedidoRegistrado> Obtener()
        {
            var json = Session.GetString(CLAVE);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<PedidoRegistrado>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<PedidoRegistrado>>(json, OpcionesJson)
                       ?? new List<PedidoRegistrado>();
            }
            catch (JsonException)
            {
                Session.Remove(CLAVE);
                return Array.Empty<PedidoRegistrado>();
            }
        }

        public void Registrar(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                return;
            }

            var lista = Obtener().ToList();

            lista.RemoveAll(p => p.PedidoId == pedidoId);
            lista.Insert(0, new PedidoRegistrado { PedidoId = pedidoId, Fecha = DateTime.Now });

            Guardar(lista.Take(TOPE));
        }

        public void Quitar(int pedidoId)
        {
            Guardar(Obtener().Where(p => p.PedidoId != pedidoId));
        }

        public void Vaciar()
        {
            Session.Remove(CLAVE);
        }

        private void Guardar(IEnumerable<PedidoRegistrado> pedidos)
        {
            Session.SetString(CLAVE, JsonSerializer.Serialize(pedidos, OpcionesJson));
        }
    }
}