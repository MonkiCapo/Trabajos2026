using Microsoft.AspNetCore.Mvc;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios;

namespace MVC.Pizzeria.Controllers;

public class PedidoController : Controller
{
    private readonly ICarritoStore _carrito;
    private readonly IMisPedidosStore _misPedidos;
    private readonly PizzeriaApiClient _api;

    public PedidoController(ICarritoStore carrito, IMisPedidosStore misPedidos, PizzeriaApiClient api)
    {
        _carrito = carrito;
        _misPedidos = misPedidos;
        _api = api;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int pizzaId, string? tamanio, int cantidad = 1)
    {
        if (cantidad < 1)
        {
            cantidad = 1;
        }

        var resultado = await _api.ObtenerPizzasAsync();

        if (!resultado.Exito)
        {
            TempData["Error"] = $"{resultado.Errores[0].Mensaje} No pudimos agregar la pizza.";
            return RedirectToAction("Productos", "Home");
        }

        var pizza = resultado.Datos!.FirstOrDefault(p => p.Id == pizzaId);

        if (pizza is null)
        {
            TempData["Error"] = "Esa pizza no existe en el catalogo.";
            return RedirectToAction("Productos", "Home");
        }

        // El tamaño elegido se valida contra la API: si no existe (o llega
        // vacio) se usa el primero de la lista, que es el tamaño por defecto.
        var tamanios = await ObtenerTamaniosAsync();
        var tamanioElegido = tamanios.FirstOrDefault(t =>
            string.Equals(t.Nombre, tamanio, StringComparison.OrdinalIgnoreCase)) ?? tamanios.First();

        _carrito.Agregar(new ItemCarritoViewModel
        {
            PizzaId = pizza.Id,
            Nombre = pizza.Nombre,
            Tamanio = tamanioElegido.Nombre,
            Precio = Math.Round(pizza.Precio * tamanioElegido.Multiplicador, 2, MidpointRounding.AwayFromZero),
            ImagenUrl = PizzaImagen.UrlDe(pizza.Id)
        }, cantidad);

        TempData["Ok"] = $"Agregamos {cantidad} × {pizza.Nombre} ({tamanioElegido.Nombre}) al carrito.";

        return RedirectToAction("Productos", "Home");
    }

    // "Arma tu pizza": la vista muestra los ingredientes de la pizza tildados
    // (destildar = quitar) y el resto del catalogo como extras con recargo.
    // El tamaño llega desde el selector del catalogo; si no es valido se usa
    // el tamaño por defecto (Grande).
    [HttpGet]
    public async Task<IActionResult> Personalizar(int pizzaId, string? tamanio)
    {
        if (pizzaId <= 0) return RedirectToAction("Productos", "Home");

        var modelo = await ArmarPersonalizarAsync(pizzaId, tamanio);
        if (modelo is null)
        {
            return RedirectToAction("Productos", "Home");
        }

        // Por defecto vienen todos los ingredientes incluidos.
        modelo.Incluir = new List<string>(modelo.IngredientesPizza);

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Personalizar(PersonalizarViewModel model)
    {
        if (model.PizzaId <= 0) return RedirectToAction("Productos", "Home");

        // Nunca se confia en el POST: la pizza, el tamaño y el catalogo de
        // ingredientes se vuelven a pedir a la API y las listas se sanean
        // contra ellos.
        var armado = await ArmarPersonalizarAsync(model.PizzaId, model.Tamanio);
        if (armado is null)
        {
            return RedirectToAction("Productos", "Home");
        }

        armado.Cantidad = model.Cantidad;

        if (!string.IsNullOrWhiteSpace(model.Tamanio)
            && !string.Equals(model.Tamanio, armado.Tamanio, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Tamanio), "El tamaño elegido no existe.");
        }

        var incluir = (model.Incluir ?? new List<string>())
            .Where(n => armado.IngredientesPizza.Contains(n, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var extras = (model.Extras ?? new List<string>())
            .Where(n => armado.ExtrasDisponibles.Contains(n, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (extras.Count > PreciosAjuste.MaxExtras)
        {
            ModelState.AddModelError(nameof(model.Extras),
                $"Solo se permiten hasta {PreciosAjuste.MaxExtras} extras por pizza.");
        }

        if (!ModelState.IsValid)
        {
            armado.Incluir = incluir;
            armado.Extras = extras;
            return View(armado);
        }

        var quitar = armado.IngredientesPizza
            .Where(ing => !incluir.Contains(ing, StringComparer.OrdinalIgnoreCase))
            .ToList();

        _carrito.Agregar(new ItemCarritoViewModel
        {
            PizzaId = armado.PizzaId,
            Nombre = armado.Nombre,
            Tamanio = armado.Tamanio,
            Precio = armado.Precio,
            ImagenUrl = armado.ImagenUrl,
            Quitar = quitar,
            Extras = extras
        }, armado.Cantidad);

        TempData["Ok"] = $"Agregamos {armado.Cantidad} × {armado.Nombre} personalizada al carrito.";
        return RedirectToAction(nameof(Resumen));
    }

    // Carga la pizza, el tamaño y el catalogo de ingredientes para el
    // formulario. Si la pizza no existe o la API no responde, deja el error
    // en TempData.
    private async Task<PersonalizarViewModel?> ArmarPersonalizarAsync(int pizzaId, string? tamanio)
    {
        var pizzasResultado = await _api.ObtenerPizzasAsync();
        if (!pizzasResultado.Exito)
        {
            TempData["Error"] = $"{pizzasResultado.Errores[0].Mensaje} No pudimos armar la personalizacion.";
            return null;
        }

        var pizza = pizzasResultado.Datos!.FirstOrDefault(p => p.Id == pizzaId);
        if (pizza is null)
        {
            TempData["Error"] = "Esa pizza no existe en el catalogo.";
            return null;
        }

        var ingredientesResultado = await _api.ObtenerIngredientesAsync();
        if (!ingredientesResultado.Exito)
        {
            TempData["Error"] = $"{ingredientesResultado.Errores[0].Mensaje} No pudimos armar la personalizacion.";
            return null;
        }

        // Tamaño invalido o vacio: se usa el primero de la lista (Grande).
        var tamanios = await ObtenerTamaniosAsync();
        var tamanioElegido = tamanios.FirstOrDefault(t =>
            string.Equals(t.Nombre, tamanio, StringComparison.OrdinalIgnoreCase)) ?? tamanios.First();

        return new PersonalizarViewModel
        {
            PizzaId = pizza.Id,
            Nombre = pizza.Nombre,
            Tamanio = tamanioElegido.Nombre,
            Precio = Math.Round(pizza.Precio * tamanioElegido.Multiplicador, 2, MidpointRounding.AwayFromZero),
            ImagenUrl = PizzaImagen.UrlDe(pizza.Id),
            IngredientesPizza = pizza.Ingredientes,
            ExtrasDisponibles = ingredientesResultado.Datos!
                .Select(i => i.Nombre)
                .Where(n => !pizza.Ingredientes.Contains(n, StringComparer.OrdinalIgnoreCase))
                .ToList()
        };
    }

    // Tamanios para el selector y los precios. Si la API no responde, se usa
    // solo "Grande" (multiplicador 1): la API igual valida el tamaño elegido
    // al confirmar el pedido.
    private async Task<List<Tamanio>> ObtenerTamaniosAsync()
    {
        var resultado = await _api.ObtenerTamaniosAsync();
        return resultado.Exito && resultado.Datos is { Count: > 0 }
            ? resultado.Datos
            : new List<Tamanio> { new() { Id = 1, Nombre = "Grande", Multiplicador = 1m } };
    }

    // Calculadora de hambre: cuestionario que sugiere cuantas pizzas de cada
    // tamanio pedir segun personas, ninos y nivel de hambre. Usa las reglas
    // de Core (CalculadoraHambre) y los precios reales de la API; no guarda
    // nada en la base ni en la sesion.
    [HttpGet]
    public async Task<IActionResult> Calculadora()
    {
        var modelo = await ArmarCalculadoraAsync();
        if (modelo is null)
        {
            return RedirectToAction("Productos", "Home");
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculadora(CalculadoraViewModel model)
    {
        var modelo = await ArmarCalculadoraAsync(model);
        if (modelo is null)
        {
            return RedirectToAction("Productos", "Home");
        }

        return View(modelo);
    }

    // "Agregar al carrito" desde la calculadora: la sugerencia se recalcula
    // SIEMPRE desde las reglas y los datos de la API; del POST solo se usan
    // personas, ninos, hambre y la pizza elegida.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculadoraAgregar(CalculadoraViewModel model)
    {
        var pizzasResultado = await _api.ObtenerPizzasAsync();
        if (!pizzasResultado.Exito)
        {
            TempData["Error"] = $"{pizzasResultado.Errores[0].Mensaje} No pudimos usar la calculadora.";
            return RedirectToAction(nameof(Calculadora));
        }

        var pizza = pizzasResultado.Datos!.FirstOrDefault(p => p.Id == model.PizzaId);
        if (pizza is null)
        {
            TempData["Error"] = "Elegi una pizza del catalogo para agregar.";
            return RedirectToAction(nameof(Calculadora));
        }

        var tamanios = await ObtenerTamaniosAsync();
        var sugerencia = CalculadoraHambre.Sugerir(model.Personas, model.Ninos, model.NivelHambre, tamanios);

        if (sugerencia.Count == 0)
        {
            TempData["Error"] = "Con esos datos no podemos armar una sugerencia.";
            return RedirectToAction(nameof(Calculadora));
        }

        var tamaniosPorNombre = tamanios.ToDictionary(t => t.Nombre, StringComparer.OrdinalIgnoreCase);
        var totalPizzas = 0;

        foreach (var linea in sugerencia)
        {
            if (!tamaniosPorNombre.TryGetValue(linea.Tamanio, out var tamanio))
            {
                continue;
            }

            _carrito.Agregar(new ItemCarritoViewModel
            {
                PizzaId = pizza.Id,
                Nombre = pizza.Nombre,
                Tamanio = tamanio.Nombre,
                Precio = Math.Round(pizza.Precio * tamanio.Multiplicador, 2, MidpointRounding.AwayFromZero),
                ImagenUrl = PizzaImagen.UrlDe(pizza.Id)
            }, linea.Cantidad);

            totalPizzas += linea.Cantidad;
        }

        TempData["Ok"] = $"La calculadora sumo {totalPizzas} pizza(s) de {pizza.Nombre} al carrito.";
        return RedirectToAction(nameof(Resumen));
    }

    // Carga pizzas y tamanios para la calculadora; si la API no responde deja
    // el error en TempData y devuelve null. Con el modelo previo (POST) se
    // respetan los datos ingresados pero la sugerencia se recalcula siempre.
    private async Task<CalculadoraViewModel?> ArmarCalculadoraAsync(CalculadoraViewModel? previo = null)
    {
        var pizzasResultado = await _api.ObtenerPizzasAsync();
        if (!pizzasResultado.Exito)
        {
            TempData["Error"] = $"{pizzasResultado.Errores[0].Mensaje} No pudimos cargar la calculadora.";
            return null;
        }

        var pizzas = pizzasResultado.Datos!
            .Select(p => new PizzaViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Tamanio = p.Tamanio,
                Precio = p.Precio,
                Ingredientes = p.Ingredientes,
                ImagenUrl = PizzaImagen.UrlDe(p.Id)
            })
            .ToList();

        var tamanios = await ObtenerTamaniosAsync();

        var modelo = previo ?? new CalculadoraViewModel();
        modelo.Pizzas = pizzas;
        modelo.Tamanios = tamanios;

        if (ModelState.IsValid)
        {
            modelo.Sugerencia = CalculadoraHambre.Sugerir(modelo.Personas, modelo.Ninos, modelo.NivelHambre, tamanios);
            modelo.PorcionesNecesarias = CalculadoraHambre.TotalPorciones(modelo.Personas, modelo.Ninos, modelo.NivelHambre);
        }
        else
        {
            modelo.Sugerencia = new List<SugerenciaTamanio>();
            modelo.PorcionesNecesarias = 0;
        }

        if (modelo.PizzaId > 0)
        {
            var pizza = pizzas.FirstOrDefault(p => p.Id == modelo.PizzaId);
            if (pizza is not null)
            {
                modelo.PrecioTotal = CalcularPrecioSugerencia(pizza.Precio, modelo.Sugerencia, tamanios);
            }
        }

        return modelo;
    }

    // Precio estimado de la sugerencia para una pizza concreta: la suma de
    // cada linea con su multiplicador. Es solo informativo: al confirmar el
    // pedido la API recalcula el precio final.
    private static decimal CalcularPrecioSugerencia(decimal precioBase, List<SugerenciaTamanio> sugerencia, List<Tamanio> tamanios)
    {
        var porNombre = tamanios.ToDictionary(t => t.Nombre, StringComparer.OrdinalIgnoreCase);
        var total = 0m;

        foreach (var linea in sugerencia)
        {
            if (!porNombre.TryGetValue(linea.Tamanio, out var tamanio))
            {
                continue;
            }

            total += linea.Cantidad * Math.Round(precioBase * tamanio.Multiplicador, 2, MidpointRounding.AwayFromZero);
        }

        return total;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Actualizar(string lineaId, int cantidad)
    {
        _carrito.Actualizar(lineaId, cantidad);
        return RedirectToAction(nameof(Resumen));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Quitar(string lineaId)
    {
        _carrito.Quitar(lineaId);
        return RedirectToAction(nameof(Resumen));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Vaciar()
    {
        _carrito.Vaciar();
        return RedirectToAction("Productos", "Home");
    }

    public IActionResult Resumen()
    {
        // El carrito vive en la sesion: esta pantalla no necesita la API.
        return View(_carrito.Obtener());
    }

    [HttpGet]
    public IActionResult Confirmacion()
    {
        var carrito = _carrito.Obtener();

        if (!carrito.HayItems)
        {
            TempData["Error"] = "Tu carrito está vacío. Agrega al menos una pizza.";
            return RedirectToAction("Productos", "Home");
        }

        // Si el usuario no está autenticado, lo enviamos al login antes de confirmar
        var usuario = UsuarioSessionHelper.ObtenerUsuario(HttpContext.Session);
        if (usuario == null)
        {
            TempData["Error"] = "Para confirmar tu pedido, por favor iniciá sesión o registrate.";
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Confirmacion", "Pedido") });
        }

        var model = new ConfirmacionViewModel
        {
            Carrito = carrito,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Direccion = usuario.Direccion
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(ConfirmacionViewModel model)
    {
        // El carrito manda: si no hay items no hay pedido que confirmar.
        var carrito = _carrito.Obtener();

        if (!carrito.HayItems)
        {
            TempData["Error"] = "Tu carrito está vacío. Agrega al menos una pizza.";
            return RedirectToAction("Productos", "Home");
        }

        var usuario = UsuarioSessionHelper.ObtenerUsuario(HttpContext.Session);
        if (usuario == null)
        {
            TempData["Error"] = "Debes iniciar sesión para confirmar tu pedido.";
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Confirmacion", "Pedido") });
        }

        // Se asignan los datos del usuario logueado
        model.Carrito = carrito;
        model.Nombre = usuario.Nombre;
        model.Email = usuario.Email;
        if (string.IsNullOrWhiteSpace(model.Telefono)) model.Telefono = usuario.Telefono;
        if (string.IsNullOrWhiteSpace(model.Direccion)) model.Direccion = usuario.Direccion;

        if (!ModelState.IsValid)
        {
            return View(nameof(Confirmacion), model);
        }

        // Los items salen de la SESION, nunca del formulario: un POST
        // manipulado no puede meter pizzas que el usuario no agrego.
        // Y la API igual recalcula cada precio desde PIZZA antes de cobrar.
        // El cliente ya lo identifica la API con el JWT (claim ClienteId):
        // el email no viaja mas en el cuerpo.
        var request = new CheckoutRequest
        {
            Nombre = model.Nombre,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Items = carrito.Items
                .Select(i => new ItemRequest
                {
                    PizzaNombre = i.Nombre,
                    Cantidad = i.Cantidad,
                    Tamanio = i.Tamanio,
                    Quitar = i.Quitar,
                    Extras = i.Extras
                })
                .ToList()
        };

        var resultado = await _api.CheckoutAsync(request);

        if (!resultado.Exito)
        {
            foreach (var (clave, mensaje) in resultado.Errores)
            {
                ModelState.AddModelError(clave, mensaje);
            }
            return View(nameof(Confirmacion), model);
        }

        var creado = resultado.Datos;
        var pedidoId = creado?.PedidoId ?? 0;

        if (pedidoId <= 0)
        {
            ModelState.AddModelError(string.Empty,
                "La API confirmo el pedido pero no devolvio el numero de pedido.");
            return View(nameof(Confirmacion), model);
        }

        // El pedido ya quedo persistido en PEDIDO + ITEM_PEDIDO:
        // el carrito cumplio su funcion.
        _carrito.Vaciar();

        // Y se deja anotado en la sesion para poder volver a encontrarlo
        // desde "Mis pedidos".
        _misPedidos.Registrar(pedidoId);

        TempData["Ok"] = "Tu pedido fue confirmado.";

        // PRG: se redirige por id y esta pantalla vuelve a consultar la
        // API, asi que el F5 no re-postea el pedido.
        return RedirectToAction(nameof(Confirmado), new { id = pedidoId });
    }

    public async Task<IActionResult> Confirmado(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction("Productos", "Home");
        }

        var resultado = await _api.ObtenerPedidoAsync(id);

        if (!resultado.Exito || resultado.Datos is null)
        {
            return RedirectToAction("Productos", "Home");
        }

        return View(resultado.Datos);
    }

    public async Task<IActionResult> MisPedidos()
    {
        if (UsuarioSessionHelper.ObtenerUsuario(HttpContext.Session) is null)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(MisPedidos)) });
        }

        var guardados = _misPedidos.Obtener();

        if (guardados.Count == 0)
        {
            return View(new MisPedidosViewModel());
        }

        async Task<(PedidoResumen? Resumen, PedidoRegistrado Guardado, string? Error)> Cargar(PedidoRegistrado guardado)
        {
            var resultado = await _api.ObtenerPedidoAsync(guardado.PedidoId);

            if (!resultado.Exito)
            {
                // 404 es el UNICO caso en el que el pedido ya no existe y se
                // puede sacar de la lista local. Cualquier otra falla (API
                // caida, sesion vencida, 500) se informa y NO se toca la
                // sesion: borrar pedidos por un error transitorio los
                // perderia para siempre.
                return resultado.CodigoEstado == 404
                    ? (null, guardado, null)
                    : (null, guardado, resultado.SinConexion
                        ? resultado.Errores[0].Mensaje
                        : $"No pudimos consultar el pedido #{guardado.PedidoId}. {resultado.Errores[0].Mensaje}");
            }

            var pedido = resultado.Datos;

            if (pedido is null)
            {
                return (null, guardado, null);
            }

            return (new PedidoResumen
            {
                PedidoId = pedido.PedidoId,
                FechaCreacion = pedido.FechaCreacion,
                Estado = pedido.Estado,
                Total = pedido.Total,
                Cliente = pedido.Cliente?.Nombre ?? "-",
                Email = pedido.Cliente?.Email ?? "-",
                CantidadPizzas = pedido.Items.Sum(i => i.Cantidad),
                Items = string.Join(", ", pedido.Items.Select(TextoDeItem))
            }, guardado, null);

            static string TextoDeItem(ItemPedidoResponse item)
            {
                var texto = $"{item.Cantidad} × {item.Pizza}";
                if (!string.IsNullOrWhiteSpace(item.Tamanio))
                {
                    texto += $" ({item.Tamanio})";
                }

                var ajustes = item.TextoAjustes;
                return ajustes.Length == 0 ? texto : $"{texto} ({ajustes})";
            }
        }

        var cargados = await Task.WhenAll(guardados.Select(Cargar));

        // Si alguna consulta fallo por algo que no sea un 404, se muestra el
        // error y se conserva la lista tal cual estaba en la sesion.
        var conError = cargados.FirstOrDefault(c => c.Error is not null);
        if (conError.Error is not null)
        {
            TempData["Error"] = conError.Error;
            return View(new MisPedidosViewModel());
        }

        // Pedidos que ya no existen en la API (404): se sacan de la sesion para no dejarlos fantasma en la lista.
        foreach (var caido in cargados.Where(c => c.Resumen is null))
        {
            _misPedidos.Quitar(caido.Guardado.PedidoId);
        }

        var lista = cargados
            .Where(c => c.Resumen is not null)
            .Select(c => c.Resumen!)
            .OrderByDescending(p => p.FechaCreacion)
            .ToList();

        return View(new MisPedidosViewModel { Pedidos = lista });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult OlvidarPedido(int id)
    {
        if (id > 0)
        {
            _misPedidos.Quitar(id);
            TempData["Ok"] = $"El pedido #{id} ya no aparece en tu lista.";
        }

        return RedirectToAction(nameof(MisPedidos));
    }

    // "Repetir pedido": re-arma el carrito desde los items guardados del
    // pedido (incluidos los ajustes) y manda directo al resumen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Repetir(int id)
    {
        if (id <= 0) return RedirectToAction(nameof(MisPedidos));

        if (UsuarioSessionHelper.ObtenerUsuario(HttpContext.Session) is null)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(MisPedidos)) });
        }

        var pedidoResultado = await _api.ObtenerPedidoAsync(id);

        if (!pedidoResultado.Exito || pedidoResultado.Datos is null)
        {
            if (pedidoResultado.SinConexion || pedidoResultado.CodigoEstado == 401)
            {
                TempData["Error"] = pedidoResultado.Errores[0].Mensaje;
            }
            else
            {
                TempData["Error"] = "No pudimos repetir ese pedido.";
            }
            return RedirectToAction(nameof(MisPedidos));
        }

        // El catalogo trae el precio base, el tamanio y la imagen actuales.
        var catalogoResultado = await _api.ObtenerPizzasAsync();

        if (!catalogoResultado.Exito)
        {
            TempData["Error"] = $"{catalogoResultado.Errores[0].Mensaje} No pudimos repetir el pedido.";
            return RedirectToAction(nameof(MisPedidos));
        }

        var pedido = pedidoResultado.Datos;

        // El tamaño guardado en cada item se revalida contra la API; si ya no
        // existiera se usa el tamaño por defecto (Grande).
        var tamanios = await ObtenerTamaniosAsync();

        foreach (var item in pedido.Items)
        {
            var pizza = catalogoResultado.Datos!.FirstOrDefault(p => p.Id == item.PizzaId);

            if (pizza is null)
            {
                TempData["Error"] = $"El pedido incluye '{item.Pizza}', que ya no esta en el catalogo.";
                return RedirectToAction(nameof(MisPedidos));
            }

            var tamanio = tamanios.FirstOrDefault(t =>
                string.Equals(t.Nombre, item.Tamanio, StringComparison.OrdinalIgnoreCase)) ?? tamanios.First();

            _carrito.Agregar(new ItemCarritoViewModel
            {
                PizzaId = pizza.Id,
                Nombre = pizza.Nombre,
                Tamanio = tamanio.Nombre,
                Precio = Math.Round(pizza.Precio * tamanio.Multiplicador, 2, MidpointRounding.AwayFromZero),
                ImagenUrl = PizzaImagen.UrlDe(pizza.Id),
                Quitar = item.Quitar,
                Extras = item.Extras
            }, item.Cantidad);
        }

        TempData["Ok"] = $"Re-armamos tu carrito con el pedido #{id}. Revisalo y confirma.";
        return RedirectToAction(nameof(Resumen));
    }

    public async Task<IActionResult> Seguimiento(int id)
    {
        if (id <= 0)
        {
            return RedirectToAction("Productos", "Home");
        }

        var pedidoResultado = await _api.ObtenerPedidoAsync(id);

        if (!pedidoResultado.Exito || pedidoResultado.Datos is null)
        {
            // Sin conexion o sesion vencida (401): el mensaje le sirve al
            // usuario. Con 404/403 no se revela nada y se redirige en seco.
            if (pedidoResultado.SinConexion || pedidoResultado.CodigoEstado == 401)
            {
                TempData["Error"] = pedidoResultado.Errores[0].Mensaje;
            }
            return RedirectToAction("Productos", "Home");
        }

        var historialResultado = await _api.ObtenerHistorialAsync(id);

        if (!historialResultado.Exito)
        {
            if (historialResultado.SinConexion || historialResultado.CodigoEstado == 401)
            {
                TempData["Error"] = historialResultado.Errores[0].Mensaje;
            }
            return RedirectToAction("Productos", "Home");
        }

        // La API ya devuelve el historial de mas viejo a mas nuevo y la vista
        // lo pinta en ese orden, asi que no hay que reordenar nada aca.
        return View(new SeguimientoViewModel
        {
            Pedido = pedidoResultado.Datos,
            Historial = historialResultado.Datos!
        });
    }
}
