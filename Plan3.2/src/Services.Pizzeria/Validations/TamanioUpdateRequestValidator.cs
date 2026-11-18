using FluentValidation;
using Core.Pizzeria.DTOs;

namespace Services.Pizzeria.Validations;

public class TamanioUpdateRequestValidator : AbstractValidator<TamanioUpdateRequest>
{
    public TamanioUpdateRequestValidator()
    {
        // El multiplicador escala el precio base de la pizza: nunca gratis
        // (0) ni un aumento desmedido (mas de x3). DECIMAL(4,2) de la base.
        RuleFor(t => t.Multiplicador)
            .InclusiveBetween(0.10m, 3.00m)
            .WithMessage("El multiplicador debe estar entre 0.10 y 3.00.")
            .PrecisionScale(4, 2, false)
            .WithMessage("El multiplicador admite como maximo 2 decimales.");
    }
}
