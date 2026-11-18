using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Services.Pizzeria.Validations;

public class PizzaRequestValidator : AbstractValidator<PizzaRequest>
{
    public PizzaRequestValidator()
    {
        RuleFor(p => p.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres.");

        RuleFor(p => p.Tamanio)
            .NotEmpty().WithMessage("El tamaño es obligatorio.")
            .MaximumLength(50).WithMessage("El tamaño no puede superar 50 caracteres.");

        // DECIMAL(10,2) de la BD: mayor a 0 y como maximo 2 decimales.
        RuleFor(p => p.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.")
            .PrecisionScale(10, 2, false).WithMessage("El precio admite como máximo 2 decimales.");

        RuleFor(p => p.Ingredientes)
            .NotNull().WithMessage("La lista de ingredientes es obligatoria.");

        RuleFor(p => p.Ingredientes)
            .Must(lista => lista is null || lista.Select(n => n.Trim().ToLowerInvariant()).Distinct().Count() == lista.Count)
            .WithMessage("La lista de ingredientes tiene nombres repetidos.");

        RuleForEach(p => p.Ingredientes)
            .NotEmpty().WithMessage("El nombre de un ingrediente no puede estar vacio.")
            .MaximumLength(100).WithMessage("El nombre de un ingrediente no puede superar 100 caracteres.");
    }
}
