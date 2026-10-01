using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class AuthService : IAuthService
{
    private const int BCRYPT_WORK_FACTOR = 11;

    private readonly IUsuarioRepositorio _usuarioRepo;
    private readonly IClienteRepositorio _clienteRepo;
    private readonly IAdo _ado;
    private readonly IValidator<RegistroRequest> _validadorRegistro;
    private readonly IValidator<LoginRequest> _validadorLogin;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepositorio usuarioRepo,
        IClienteRepositorio clienteRepo,
        IAdo ado,
        IValidator<RegistroRequest> validadorRegistro,
        IValidator<LoginRequest> validadorLogin,
        ILogger<AuthService> logger)
    {
        _usuarioRepo = usuarioRepo;
        _clienteRepo = clienteRepo;
        _ado = ado;
        _validadorRegistro = validadorRegistro;
        _validadorLogin = validadorLogin;
        _logger = logger;
    }

    public async Task<UsuarioResponse> RegistrarAsync(RegistroRequest request)
    {
        var validationResult = await _validadorRegistro.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // USUARIO.email y CLIENTE.email son UNIQUE: si ya existe el cliente, no hay registro
        if (await _clienteRepo.ExisteEmailDeClienteAsync(request.Email))
        {
            throw new ValidationException($"El email '{request.Email}' ya esta registrado.");
        }

        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        try
        {
            // 1. Insertar el CLIENTE
            var nuevoCliente = new Cliente
            {
                Nombre = request.Nombre,
                Email = request.Email,
                Telefono = request.Telefono,
                Direccion = request.Direccion
            };

            var clienteId = await _clienteRepo.AgregarClienteAsync(nuevoCliente, conexion, transaction);
            nuevoCliente.Id = clienteId;

            // 2. Insertar el USUARIO con la contraseña hasheada
            var nuevoUsuario = new Usuario
            {
                ClienteId = clienteId,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: BCRYPT_WORK_FACTOR),
                Rol = "Cliente",
                FechaCreacion = DateTime.UtcNow
            };

            var usuarioId = await _usuarioRepo.CrearUsuarioAsync(nuevoUsuario, conexion, transaction);
            nuevoUsuario.Id = usuarioId;

            transaction.Commit();

            _logger.LogInformation("[AUTHSERVICE] Usuario registrado: {Email} (UsuarioId: {UsuarioId}, ClienteId: {ClienteId})", nuevoUsuario.Email, usuarioId, clienteId);

            // Nunca se devuelve el PasswordHash
            return new UsuarioResponse
            {
                UsuarioId = usuarioId,
                ClienteId = clienteId,
                Nombre = nuevoCliente.Nombre,
                Email = nuevoUsuario.Email,
                Rol = nuevoUsuario.Rol
            };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "[AUTHSERVICE] Error al registrar al usuario {Email}.", request.Email);
            throw;
        }
    }

    public async Task<UsuarioResponse?> LoginAsync(LoginRequest request)
    {
        var validationResult = await _validadorLogin.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var usuario = await _usuarioRepo.ObtenerPorEmailAsync(request.Email);
        if (usuario == null)
        {
            _logger.LogWarning("[AUTHSERVICE] Intento de login con email no registrado: {Email}", request.Email);
            return null;
        }

        // Se verifica el hash contra la contraseña enviada
        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            _logger.LogWarning("[AUTHSERVICE] Contraseña incorrecta para el email {Email}", request.Email);
            return null;
        }

        var cliente = await _clienteRepo.ObtenerClientePorIdAsync(usuario.ClienteId);

        _logger.LogInformation("[AUTHSERVICE] Login exitoso: {Email} (Rol: {Rol})", usuario.Email, usuario.Rol);

        return new UsuarioResponse
        {
            UsuarioId = usuario.Id,
            ClienteId = usuario.ClienteId,
            Nombre = cliente?.Nombre ?? string.Empty,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }
}
