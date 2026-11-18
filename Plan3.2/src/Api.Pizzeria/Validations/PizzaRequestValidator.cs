using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Api.Pizzeria.Validations;

public class PizzaRequestValidator : AbstractValidator<PizzaRequest>
{
    public PizzaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la pizza es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres.");

        RuleFor(x => x.Tamanio)
            .NotEmpty().WithMessage("El tamaño es obligatorio.")
            .MaximumLength(50).WithMessage("El tamaño no puede superar 50 caracteres.");

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");
    }
}