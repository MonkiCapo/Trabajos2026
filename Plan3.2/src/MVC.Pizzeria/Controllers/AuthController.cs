using Microsoft.AspNetCore.Mvc;
using Core.Pizzeria.DTOs;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;
using MVC.Pizzeria.Servicios;

namespace MVC.Pizzeria.Controllers;

public class AuthController : Controller
{
    private readonly PizzeriaApiClient _api;
    private readonly IMisPedidosStore _misPedidos;

    public AuthController(PizzeriaApiClient api, IMisPedidosStore misPedidos)
    {
        _api = api;
        _misPedidos = misPedidos;
    }

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (UsuarioSessionHelper.EstaAutenticado(HttpContext.Session))
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Productos", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _api.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (resultado.Exito && resultado.Datos is not null)
        {
            UsuarioSessionHelper.GuardarUsuario(HttpContext.Session, resultado.Datos.Usuario);
            UsuarioSessionHelper.GuardarToken(HttpContext.Session, resultado.Datos.Token);
            TempData["Ok"] = $"¡Bienvenido de nuevo, {resultado.Datos.Usuario.Nombre}!";

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Productos", "Home");
        }

        foreach (var (clave, mensaje) in resultado.Errores)
        {
            ModelState.AddModelError(clave, mensaje);
        }

        return View(model);
    }

    // GET: /Auth/Registro
    [HttpGet]
    public IActionResult Registro(string? returnUrl = null)
    {
        if (UsuarioSessionHelper.EstaAutenticado(HttpContext.Session))
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Productos", "Home");
        }

        return View(new RegistroViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Auth/Registro
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _api.RegistroAsync(new RegistroRequest
        {
            Nombre = model.Nombre,
            Email = model.Email,
            Telefono = model.Telefono,
            Direccion = model.Direccion,
            Password = model.Password
        });

        if (resultado.Exito && resultado.Datos is not null)
        {
            UsuarioSessionHelper.GuardarUsuario(HttpContext.Session, resultado.Datos.Usuario);
            UsuarioSessionHelper.GuardarToken(HttpContext.Session, resultado.Datos.Token);
            TempData["Ok"] = $"¡Cuenta creada con éxito! Bienvenido, {resultado.Datos.Usuario.Nombre}.";

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Productos", "Home");
        }

        foreach (var (clave, mensaje) in resultado.Errores)
        {
            ModelState.AddModelError(clave, mensaje);
        }

        return View(model);
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Vaciar antes de eliminar al usuario: la clave de la lista depende del usuario logueado.
        _misPedidos.Vaciar();
        UsuarioSessionHelper.EliminarUsuario(HttpContext.Session);
        UsuarioSessionHelper.EliminarToken(HttpContext.Session);
        TempData["Ok"] = "Sesión cerrada correctamente.";
        return RedirectToAction("Index", "Home");
    }
}
