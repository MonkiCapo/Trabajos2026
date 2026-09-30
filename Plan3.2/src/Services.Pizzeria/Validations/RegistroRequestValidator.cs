using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Services.Pizzeria.Validations;

public class RegistroRequestValidator : AbstractValidator<RegistroRequest>
{
    public RegistroRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(150).WithMessage("El email no puede superar 150 caracteres.")
            .EmailAddress().WithMessage("El formato del email no es valido.");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El telefono es obligatorio.")
            .MaximumLength(20).WithMessage("El telefono no puede superar 20 caracteres.")
            .Matches(@"^[\d\s\-\+]+$").WithMessage("El telefono solo puede contener numeros, espacios, guiones y '+'.");

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La direccion es obligatoria.")
            .MaximumLength(200).WithMessage("La direccion no puede superar 200 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede superar 100 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe tener al menos una mayuscula.")
            .Matches(@"[a-z]").WithMessage("La contraseña debe tener al menos una minuscula.")
            .Matches(@"\d").WithMessage("La contraseña debe tener al menos un numero.")
            .Matches(@"[\W]").WithMessage("La contraseña debe tener al menos un simbolo.");
    }
}
