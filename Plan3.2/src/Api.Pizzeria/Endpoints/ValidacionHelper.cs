using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Api.Pizzeria.Endpoints;

public static class ValidacionHelper
{
    // Respuesta uniforme a ValidationException: ValidationProblem cuando hay
    // errores por campo y BadRequest con el mensaje cuando viene uno unico
    // (por ejemplo, email duplicado).
    public static IResult RespuestaDe(ValidationException ex)
    {
        return ex.Errors.Any()
            ? Results.ValidationProblem(ex.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()))
            : Results.BadRequest(new { error = ex.Message });
    }
}
