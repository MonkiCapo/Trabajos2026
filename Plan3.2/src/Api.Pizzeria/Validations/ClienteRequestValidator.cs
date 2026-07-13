using FluentValidation;
using Api.Pizzeria.DTOs;

namespace Api.Pizzeria.Validations;

public class ClienteRequestValidator : AbstractValidator<ClienteRequest>
{
    public ClienteRequestValidator()
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
    }
}
