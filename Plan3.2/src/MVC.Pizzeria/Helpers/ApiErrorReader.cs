using System.Text.Json;

namespace MVC.Pizzeria.Helpers
{
    public static class ApiErrorReader
    {
        public static async Task<List<(string Clave, string Mensaje)>> LeerAsync(HttpResponseMessage response)
        {
            var errores = new List<(string, string)>();
            var cuerpo = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(cuerpo))
            {
                errores.Add((string.Empty, "No se pudo completar el pedido."));
                return errores;
            }

            try
            {
                using var doc = JsonDocument.Parse(cuerpo);
                var root = doc.RootElement;

                // Formato ValidationProblem: { "errors": { "Email": ["mensaje"] } }
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var campo in errors.EnumerateObject())
                    {
                        // Las claves con indice (Items[0].Cantidad) no existen en el model del MVC
                        var clave = campo.Name.Contains('[') ? string.Empty : campo.Name;

                        foreach (var mensaje in campo.Value.EnumerateArray())
                        {
                            errores.Add((clave, mensaje.GetString() ?? "Dato invalido."));
                        }
                    }

                    return errores;
                }

                // Formato { "error": "..." } o { "error": "...", "detalles": "..." }
                if (root.TryGetProperty("error", out var error))
                {
                    var mensaje = error.GetString() ?? "Ocurrio un error.";

                    if (root.TryGetProperty("detalles", out var detalles) && detalles.ValueKind == JsonValueKind.String)
                    {
                        mensaje = $"{mensaje} {detalles.GetString()}";
                    }

                    errores.Add((string.Empty, mensaje));
                }
            }
            catch (JsonException)
            {
                errores.Add((string.Empty, "La API respondio con un error inesperado."));
            }

            if (errores.Count == 0)
            {
                errores.Add((string.Empty, $"No se pudo completar el pedido. La API respondio {(int)response.StatusCode}."));
            }

            return errores;
        }
    }
}
