using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Core.Pizzeria.DTOs;
using MVC.Pizzeria.Helpers;
using MVC.Pizzeria.Models;

namespace MVC.Pizzeria.Controllers;

public class AuthController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private string BaseUrl => _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5183";

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

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/auth/login", new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            });

            if (response.IsSuccessStatusCode)
            {
                var usuario = await response.Content.ReadFromJsonAsync<UsuarioResponse>();
                if (usuario != null)
                {
                    UsuarioSessionHelper.GuardarUsuario(HttpContext.Session, usuario);
                    TempData["Ok"] = $"¡Bienvenido de nuevo, {usuario.Nombre}!";

                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Productos", "Home");
                }
            }

            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "No pudimos conectar con el servidor de la API.");
            return View(model);
        }
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

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new RegistroRequest
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Telefono = model.Telefono,
                Direccion = model.Direccion,
                Password = model.Password
            };

            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/auth/registro", request);

            if (response.IsSuccessStatusCode)
            {
                var usuario = await response.Content.ReadFromJsonAsync<UsuarioResponse>();
                if (usuario != null)
                {
                    UsuarioSessionHelper.GuardarUsuario(HttpContext.Session, usuario);
                    TempData["Ok"] = $"¡Cuenta creada con éxito! Bienvenido, {usuario.Nombre}.";

                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Productos", "Home");
                }
            }

            var errorObj = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var errorMsg = errorObj != null && errorObj.TryGetValue("error", out var err)
                ? err?.ToString()
                : "Error al registrar la cuenta. Es posible que el email ya exista.";

            ModelState.AddModelError(string.Empty, errorMsg ?? "Error al registrarse.");
            return View(model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "No pudimos conectar con la API.");
            return View(model);
        }
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        UsuarioSessionHelper.EliminarUsuario(HttpContext.Session);
        TempData["Ok"] = "Sesión cerrada correctamente.";
        return RedirectToAction("Index", "Home");
    }
}
