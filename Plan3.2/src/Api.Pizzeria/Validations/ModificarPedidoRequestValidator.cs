using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Api.Pizzeria.Validations;

public class ModificarPedidoRequestValidator : AbstractValidator<ModificarPedidoRequest>
{
    public ModificarPedidoRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("La lista de items es obligatoria.")
            .NotEmpty().WithMessage("Debe contener al menos un item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.PizzaNombre)
                .NotEmpty().WithMessage("El nombre de la pizza es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre de la pizza no puede superar 100 caracteres.");

            item.RuleFor(i => i.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.")
                .LessThanOrEqualTo(100).WithMessage("La cantidad no puede superar 100.");
        });
    }
}
