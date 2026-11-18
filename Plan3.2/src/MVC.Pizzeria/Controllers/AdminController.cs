using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;

namespace MVC.Pizzeria.Controllers;

public class AdminController : Controller
{
    private const long MAX_TAMANIO_IMAGEN = 2 * 1024 * 1024;
    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly PizzeriaApiClient _api;
    private readonly IWebHostEnvironment _env;

    public AdminController(PizzeriaApiClient api, IWebHostEnvironment env)
    {
        _api = api;
        _env = env;
    }

    // El menu se oculta sin Rol Admin, pero el controlador lo verifica
    // igual: la garantia real la da la API con su politica Admin.
    private IActionResult? SiNoEsAdmin()
    {
        var usuario = UsuarioSessionHelper.ObtenerUsuario(HttpContext.Session);
        if (usuario is null || !string.Equals(usuario.Rol, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "No tenés permisos para acceder al panel de administración.";
            return RedirectToAction("Index", "Home");
        }

        return null;
    }

    public async Task<IActionResult> Index()
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;

        var resultado = await _api.ObtenerPizzasAsync();

        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Errores[0].Mensaje;
            return RedirectToAction("Productos", "Home");
        }

        var pizzas = resultado.Datos!
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

        return View(pizzas);
    }

    public async Task<IActionResult> Crear()
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;

        var model = await ArmarViewModelAsync();
        if (model is null)
        {
            TempData["Error"] = "No pudimos cargar los ingredientes desde la API.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PizzaAdminViewModel model)
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;

        ValidarImagen(model);

        if (!ModelState.IsValid)
        {
            await RepoblarIngredientesAsync(model);
            return View(model);
        }

        var resultado = await _api.CrearPizzaAsync(new PizzaRequest
        {
            Nombre = model.Nombre,
            Tamanio = model.Tamanio,
            Precio = model.Precio,
            Ingredientes = model.IngredientesSeleccionados
        });

        if (!resultado.Exito)
        {
            foreach (var (clave, mensaje) in resultado.Errores)
            {
                ModelState.AddModelError(clave, mensaje);
            }
            await RepoblarIngredientesAsync(model);
            return View(model);
        }

        var pizza = resultado.Datos!;

        // Paso 2 del flujo de imagen: la pizza ya quedo persistida en la API,
        // ahora se guarda el archivo con la convencion pizza-{id}.jpg.
        var avisoImagen = await GuardarImagenAsync(model.Imagen, pizza.Id);
        if (avisoImagen is not null)
        {
            TempData["Error"] = avisoImagen;
        }
        else
        {
            TempData["Ok"] = $"La pizza '{pizza.Nombre}' fue creada.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;
        if (id <= 0) return RedirectToAction(nameof(Index));

        var resultado = await _api.ObtenerPizzasAsync();

        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Errores[0].Mensaje;
            return RedirectToAction(nameof(Index));
        }

        var pizza = resultado.Datos!.FirstOrDefault(p => p.Id == id);
        if (pizza is null)
        {
            TempData["Error"] = "Esa pizza no existe.";
            return RedirectToAction(nameof(Index));
        }

        var model = await ArmarViewModelAsync(pizza);
        if (model is null)
        {
            TempData["Error"] = "No pudimos cargar los ingredientes desde la API.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(PizzaAdminViewModel model)
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;
        if (model.Id <= 0) return RedirectToAction(nameof(Index));

        ValidarImagen(model);

        if (!ModelState.IsValid)
        {
            await RepoblarIngredientesAsync(model);
            return View(model);
        }

        var resultado = await _api.ActualizarPizzaAsync(model.Id, new PizzaRequest
        {
            Nombre = model.Nombre,
            Tamanio = model.Tamanio,
            Precio = model.Precio,
            Ingredientes = model.IngredientesSeleccionados
        });

        if (!resultado.Exito)
        {
            foreach (var (clave, mensaje) in resultado.Errores)
            {
                ModelState.AddModelError(clave, mensaje);
            }
            await RepoblarIngredientesAsync(model);
            return View(model);
        }

        var pizza = resultado.Datos!;

        // Si no se sube archivo, la imagen actual queda como estaba.
        var avisoImagen = await GuardarImagenAsync(model.Imagen, pizza.Id);
        if (avisoImagen is not null)
        {
            TempData["Error"] = avisoImagen;
        }
        else
        {
            TempData["Ok"] = $"La pizza '{pizza.Nombre}' fue actualizada.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;
        if (id <= 0) return RedirectToAction(nameof(Index));

        var resultado = await _api.EliminarPizzaAsync(id);

        if (!resultado.Exito)
        {
            // El 409 trae el mensaje "tiene pedidos asociados" ya parseado.
            TempData["Error"] = resultado.Errores[0].Mensaje;
            return RedirectToAction(nameof(Index));
        }

        BorrarImagen(id);
        TempData["Ok"] = $"La pizza #{id} fue eliminada.";
        return RedirectToAction(nameof(Index));
    }

    // Pagina de tamanios: solo se edita el multiplicador de cada uno. El
    // nombre no se edita porque los items historicos lo guardan y la API
    // no expone esa operacion.
    public async Task<IActionResult> Tamanios()
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;

        var resultado = await _api.ObtenerTamaniosAsync();

        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Errores[0].Mensaje;
            return RedirectToAction(nameof(Index));
        }

        return View(resultado.Datos!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tamanios(int id, decimal multiplicador)
    {
        if (SiNoEsAdmin() is { } noAdmin) return noAdmin;

        // Validacion local espejo de la API (0.10–3.00, 2 decimales): el
        // usuario ve el error aca y la API igual vuelve a validar.
        if (multiplicador < 0.10m || multiplicador > 3.00m)
        {
            ModelState.AddModelError("multiplicador", "El multiplicador debe estar entre 0.10 y 3.00.");
        }
        else if (decimal.Round(multiplicador, 2) != multiplicador)
        {
            ModelState.AddModelError("multiplicador", "El multiplicador admite como maximo 2 decimales.");
        }

        if (!ModelState.IsValid)
        {
            return View(await CargarTamaniosAsync());
        }

        var resultado = await _api.ActualizarTamanioAsync(id, new TamanioUpdateRequest
        {
            Multiplicador = multiplicador
        });

        if (!resultado.Exito)
        {
            foreach (var (clave, mensaje) in resultado.Errores)
            {
                ModelState.AddModelError(clave, mensaje);
            }
            return View(await CargarTamaniosAsync());
        }

        var tamanio = resultado.Datos!;
        TempData["Ok"] = $"El tamaño '{tamanio.Nombre}' ahora multiplica ×{tamanio.Multiplicador:0.##}.";
        return RedirectToAction(nameof(Tamanios));
    }

    private async Task<List<Tamanio>> CargarTamaniosAsync()
    {
        var resultado = await _api.ObtenerTamaniosAsync();
        return resultado.Exito ? resultado.Datos! : new List<Tamanio>();
    }

    private async Task<PizzaAdminViewModel?> ArmarViewModelAsync(Pizza? pizza = null)
    {
        var resultado = await _api.ObtenerIngredientesAsync();
        if (!resultado.Exito || resultado.Datos is null)
        {
            return null;
        }

        return new PizzaAdminViewModel
        {
            Id = pizza?.Id ?? 0,
            Nombre = pizza?.Nombre ?? string.Empty,
            Tamanio = pizza?.Tamanio ?? string.Empty,
            Precio = pizza?.Precio ?? 0,
            IngredientesSeleccionados = pizza is null ? new List<string>() : pizza.Ingredientes,
            IngredientesDisponibles = resultado.Datos
                .Select(i => new SelectListItem { Value = i.Nombre, Text = i.Nombre })
                .ToList(),
            ImagenActual = pizza is null ? string.Empty : PizzaImagen.UrlDe(pizza.Id)
        };
    }

    // Despues de un POST fallido hay que volver a llenar el multi-select.
    private async Task RepoblarIngredientesAsync(PizzaAdminViewModel model)
    {
        var repoblado = await ArmarViewModelAsync();
        if (repoblado is not null)
        {
            model.IngredientesDisponibles = repoblado.IngredientesDisponibles;
        }
    }

    private void ValidarImagen(PizzaAdminViewModel model)
    {
        var imagen = model.Imagen;
        if (imagen is null || imagen.Length == 0)
        {
            return;
        }

        var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            ModelState.AddModelError(nameof(model.Imagen), "La imagen debe ser JPG, PNG o WEBP.");
            return;
        }

        if (imagen.Length > MAX_TAMANIO_IMAGEN)
        {
            ModelState.AddModelError(nameof(model.Imagen), "La imagen no puede superar los 2 MB.");
        }
    }

    // Devuelve null si no habia imagen o se guardo bien; un mensaje si fallo.
    private async Task<string?> GuardarImagenAsync(IFormFile? imagen, int pizzaId)
    {
        if (imagen is null || imagen.Length == 0)
        {
            return null;
        }

        try
        {
            var carpeta = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(carpeta);

            var ruta = Path.Combine(carpeta, $"pizza-{pizzaId}.jpg");
            await using var archivo = System.IO.File.Create(ruta);
            await imagen.CopyToAsync(archivo);

            return null;
        }
        catch (Exception)
        {
            return "La pizza quedó guardada sin imagen (no pudimos guardar el archivo). Podés subirla desde Editar.";
        }
    }

    private void BorrarImagen(int pizzaId)
    {
        try
        {
            var ruta = Path.Combine(_env.WebRootPath, "images", $"pizza-{pizzaId}.jpg");
            if (System.IO.File.Exists(ruta))
            {
                System.IO.File.Delete(ruta);
            }
        }
        catch (Exception)
        {
            // Best effort: si no se puede borrar, queda un jpg huerfano
            // que no rompe nada (el catálogo usa el placeholder al fallar).
        }
    }
}
