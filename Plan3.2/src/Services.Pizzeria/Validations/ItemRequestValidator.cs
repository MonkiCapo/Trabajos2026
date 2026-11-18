using FluentValidation;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Servicios;

namespace Services.Pizzeria.Validations;

// Reglas estructurales de los ajustes de "Arma tu pizza". La existencia de
// los ingredientes en la BD se valida en PedidoService, que tiene acceso a
// los repositorios dentro de la transaccion del checkout.
public class ItemRequestValidator : AbstractValidator<ItemRequest>
{
    public ItemRequestValidator()
    {
        RuleFor(i => i.Quitar)
            .NotNull().WithMessage("La lista de ingredientes a quitar es obligatoria.");

        RuleFor(i => i.Extras)
            .NotNull().WithMessage("La lista de extras es obligatoria.");

        // Opcional: vacio significa "Grande". La existencia del tamaño en la
        // BD se valida en PedidoService, dentro de la transaccion.
        RuleFor(i => i.Tamanio)
            .MaximumLength(50).WithMessage("El nombre del tamaño no puede superar 50 caracteres.");

        RuleFor(i => i.Extras)
            .Must(extras => extras.Count <= PreciosAjuste.MaxExtras)
            .WithMessage($"Solo se permiten hasta {PreciosAjuste.MaxExtras} extras por pizza.");

        RuleFor(i => i.Quitar)
            .Must(SinRepetidos).WithMessage("La lista de ingredientes a quitar tiene nombres repetidos.");

        RuleFor(i => i.Extras)
            .Must(SinRepetidos).WithMessage("La lista de extras tiene nombres repetidos.");

        RuleFor(i => i)
            .Must(SinSolapamiento).WithMessage("Un ingrediente no puede estar a la vez para quitar y como extra.");

        RuleForEach(i => i.Quitar)
            .NotEmpty().WithMessage("El nombre de un ingrediente a quitar no puede estar vacio.")
            .MaximumLength(100).WithMessage("El nombre de un ingrediente a quitar no puede superar 100 caracteres.");

        RuleForEach(i => i.Extras)
            .NotEmpty().WithMessage("El nombre de un extra no puede estar vacio.")
            .MaximumLength(100).WithMessage("El nombre de un extra no puede superar 100 caracteres.");
    }

    private static bool SinRepetidos(List<string> lista) =>
        lista.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == lista.Count;

    private static bool SinSolapamiento(ItemRequest item) =>
        !item.Quitar.Any(q => item.Extras.Contains(q, StringComparer.OrdinalIgnoreCase));
}
