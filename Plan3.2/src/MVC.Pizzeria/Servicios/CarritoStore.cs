using System.Text.Json;
using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Servicios;

public class CarritoStore : ICarritoStore
{
    public const int CANTIDAD_MAXIMA = 99;

    private const string CLAVE = "Carrito";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CarritoStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session =>
        _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException(
            "No hay sesion disponible en el contexto actual. Revisa que app.UseSession() esté en el pipeline y que AddSession() esté registrado.");

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

        // Dos lineas se fusionan solo si son la misma pizza, el mismo tamaño
        // y los mismos ajustes: "Muzzarella sin aceitunas" y "Muzzarella
        // comun", o Grande y Mediano, son lineas distintas.
        var linea = carrito.Items.FirstOrDefault(i =>
            i.PizzaId == item.PizzaId
            && string.Equals(i.Tamanio, item.Tamanio, StringComparison.OrdinalIgnoreCase)
            && MismosAjustes(i, item));

        if (linea is null)
        {
            carrito.Items.Add(new ItemCarritoViewModel
            {
                LineaId = string.IsNullOrWhiteSpace(item.LineaId)
                    ? Guid.NewGuid().ToString("N")
                    : item.LineaId,
                PizzaId = item.PizzaId,
                Nombre = item.Nombre,
                Tamanio = item.Tamanio,
                ImagenUrl = item.ImagenUrl,
                Precio = item.Precio,
                Cantidad = Math.Clamp(cantidad, 1, CANTIDAD_MAXIMA),
                Quitar = new List<string>(item.Quitar),
                Extras = new List<string>(item.Extras)
            });
        }
        else
        {
            linea.Cantidad = Math.Clamp(linea.Cantidad + cantidad, 1, CANTIDAD_MAXIMA);
        }

        Guardar(carrito);
    }

    public void Actualizar(string lineaId, int cantidad)
    {
        var carrito = Obtener();

        if (cantidad <= 0)
        {
            carrito.Items.RemoveAll(i => i.LineaId == lineaId);
        }
        else
        {
            var linea = carrito.Items.FirstOrDefault(i => i.LineaId == lineaId);
            if (linea is not null)
            {
                linea.Cantidad = Math.Min(cantidad, CANTIDAD_MAXIMA);
            }
        }

        Guardar(carrito);
    }

    public void Quitar(string lineaId)
    {
        var carrito = Obtener();
        carrito.Items.RemoveAll(i => i.LineaId == lineaId);
        Guardar(carrito);
    }

    public void Vaciar()
    {
        Session.Remove(CLAVE);
    }

    private static bool MismosAjustes(ItemCarritoViewModel a, ItemCarritoViewModel b) =>
        a.Quitar.Count == b.Quitar.Count
        && a.Extras.Count == b.Extras.Count
        && a.Quitar.All(q => b.Quitar.Contains(q, StringComparer.OrdinalIgnoreCase))
        && a.Extras.All(e => b.Extras.Contains(e, StringComparer.OrdinalIgnoreCase));
}
