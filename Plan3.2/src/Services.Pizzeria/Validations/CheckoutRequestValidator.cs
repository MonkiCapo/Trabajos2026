using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Services.Pizzeria.Validations;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
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

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("El carrito no puede estar vacio. Agrega al menos una pizza.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.PizzaNombre)
                .NotEmpty().WithMessage("Toda pizza del carrito debe tener nombre.");

            item.RuleFor(i => i.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.")
                .LessThanOrEqualTo(99).WithMessage("La cantidad maxima por pizza es 99.");
        });
    }
}
