using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Services.Pizzeria.Validations;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(150).WithMessage("El email no puede superar 150 caracteres.")
            .EmailAddress().WithMessage("El formato del email no es valido.");

        // Solo se valida que venga informada: las reglas de complejidad
        // se aplican al registrarse, no al iniciar sesion.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(100).WithMessage("La contraseña no puede superar 100 caracteres.");
    }
}
