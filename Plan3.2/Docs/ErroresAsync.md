# Análisis de Manejo de Errores Asincrónicos en C#

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Introducción

En un sistema cliente-servidor como PizzeriaAPI, las operaciones de red (HTTP) y de base de datos (MySQL) son inherentemente asíncronas. C# proporciona `async`/`await` para trabajar con operaciones no bloqueantes, pero el manejo de errores en este contexto tiene particularidades que deben tenerse en cuenta.

---

## 2. Patrón básico: async/await + try/catch

### 2.1 Llamada HTTP desde el cliente (Consola.Cliente)

```csharp
public async Task<PedidoDetalle> ConsultarPedidoAsync(int pedidoId, CancellationToken ct)
{
    using var httpClient = _factory.CreateClient();
    httpClient.Timeout = TimeSpan.FromSeconds(10);

    try
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"http://localhost:5183/api/pedidos/{pedidoId}", ct);

        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PedidoDetalle>(body);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"[ERROR] No se pudo conectar a la API: {ex.Message}");
        throw new ServicioNoDisponibleException("La API no esta disponible");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("[ERROR] Timeout de conexion");
        throw new ServicioNoDisponibleException("El servidor no respondio a tiempo");
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"[ERROR] Respuesta mal formada: {ex.Message}");
        throw new RespuestaInvalidaException("La respuesta del servidor no es valida");
    }
}
```

**Análisis de buenas prácticas:**
- ✅ Se capturan excepciones específicas en lugar de usar `catch (Exception)` genérico.
- ✅ Se registra el error en log antes de relanzar.
- ✅ Se transforman excepciones técnicas en excepciones de dominio (`ServicioNoDisponibleException`).
- ✅ `Timeout` explícito en `HttpClient` + `CancellationToken` propagado desde el llamador.

---

### 2.2 Acceso a la base de datos con Dapper + MySqlConnector

```csharp
public async Task<IEnumerable<Pizza>> ObtenerDisponiblesAsync(CancellationToken ct)
{
    const string sql = "SELECT id, nombre, tamanio, precio, descripcion, stock, disponible " +
                       "FROM PIZZA WHERE disponible = 1 ORDER BY nombre;";

    using var conexion = await AbrirConexionAsync(ct);

    try
    {
        return await conexion.QueryAsync<Pizza>(
            new CommandDefinition(sql, cancellationToken: ct));
    }
    catch (MySqlException ex)
    {
        Console.WriteLine($"[ERROR] Fallo de MySQL: {ex.Number} - {ex.Message}");
        throw;
    }
}
```

**Análisis de buenas prácticas:**
- ✅ `OpenAsync` y `CommandDefinition` con `CancellationToken`: si el request HTTP se cancela, la consulta se aborta.
- ✅ `MySqlException` tiene `Number` (código de error del servidor), útil para distinguir casos (p. ej. 1062 = clave duplicada).
- ✅ `using var conexion` garantiza el cierre de la conexión al pool aunque falle la query.

---

### 2.3 Transacción asíncrona con rollback (PedidoService)

```csharp
await using var conexion = _ado.GetDbConnection();
await conexion.OpenAsync(ct);
await using var transaction = await conexion.BeginTransactionAsync(ct);

try
{
    // 1. Descuento atómico de stock por pizza
    // 2. INSERT pedido (EsperaConfirmacion)
    // 3. Multi-INSERT de ítems
    // 4. INSERT historial de estado

    await transaction.CommitAsync(ct);
}
catch (ArgumentException)
{
    await transaction.RollbackAsync(ct);
    throw; // el middleware lo traduce a 400
}
catch (Exception ex)
{
    await transaction.RollbackAsync(ct);
    _logger.LogError(ex, "[PEDIDOSERVICE] Error al crear el pedido; transaccion revertida.");
    throw;
}
```

**Análisis de buenas prácticas:**
- ✅ `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`: toda la cadena es asíncrona, sin bloquear hilos.
- ✅ Rollback en **cualquier** fallo: nunca queda stock descontado sin pedido persistido.
- ✅ El error esperado (`ArgumentException`) se relanza sin ruido; el inesperado se loguea con stack trace.
- ✅ El `CancellationToken` se propaga a cada operación dentro de la transacción.

---

## 3. Traducción centralizada: ExceptionHandlingMiddleware

En lugar de repetir `try/catch` en cada endpoint, la API centraliza la traducción de excepciones:

```csharp
catch (PedidoNoEncontradoException)
{
    await EscribirJsonAsync(context, 404,
        new { error = "Pedido no encontrado." });
}
catch (TransicionInvalidaException ex)
{
    await EscribirJsonAsync(context, 409,
        new { error = ex.Message,
              estadoActual = ex.EstadoActual.ToString(),
              estadoSolicitado = ex.EstadoSolicitado.ToString() });
}
catch (MySqlException ex) when (ex.Number == 1062)
{
    await EscribirJsonAsync(context, 409,
        new { error = "Ya existe un registro con esos datos..." });
}
catch (ArgumentException ex)
{
    await EscribirJsonAsync(context, 400,
        new { error = "Datos invalidos", detalles = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "[MIDDLEWARE] Error no controlado: {Path}", context.Request.Path);
    await EscribirJsonAsync(context, 500,
        new { error = "Ocurrio un error inesperado..." });
}
```

**Análisis:**
- ✅ **Filtros de excepción (`when`):** permiten distinguir el error 1062 de MySQL sin anidar catch.
- ✅ **Orden de captura:** de lo más específico a lo más general; el `Exception` genérico va último.
- ✅ **Respuesta uniforme:** todos los errores salen como JSON con el mismo formato.
- ✅ **Log solo para 500:** los errores esperados (400/404/409) no ensucian el log.

---

## 4. Escenario: API caída desde la Consola.Cliente

```csharp
public static async Task ProbarConexionAsync()
{
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

    try
    {
        var response = await httpClient.GetAsync("http://localhost:5183/api/pizzas");
        response.EnsureSuccessStatusCode();
        Console.WriteLine("API disponible.");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Fallo capturado: {ex.Message}");
        Console.WriteLine("Accion: verificar que la API este corriendo en el puerto 5183");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Timeout: la API no respondio en 5 segundos.");
        Console.WriteLine("Accion: reintentar mas tarde");
    }
}
```

---

## 5. Buenas prácticas resumidas

| Práctica | Descripción |
|----------|-------------|
| **Usar CancellationToken** | Siempre propagar el token a operaciones async de red y base de datos. |
| **Timeouts explícitos** | No confiar en valores por defecto; establecerlos (`HttpClient.Timeout`, `CancelAfter`). |
| **Capturar excepciones específicas** | Preferir `HttpRequestException`, `TaskCanceledException`, `MySqlException` sobre `Exception` genérico. |
| **Filtros de excepción (`when`)** | Permiten distinguir códigos de error (p. ej. `MySqlException.Number == 1062`) sin anidar catch. |
| **Log antes de relanzar** | Registrar el error en el punto de captura antes de propagar hacia arriba. |
| **No mezclar sync con async** | Evitar `.Result` o `.Wait()`; usar `await` en toda la cadena. |
| **`using` en recursos IDisposable** | `HttpClient`, `DbConnection`, `DbTransaction` deben liberarse. |
| **Transacciones asíncronas** | `BeginTransactionAsync` + `CommitAsync`/`RollbackAsync` para escrituras multi-tabla. |

---

## 6. Conclusión

El manejo de errores asincrónicos en C# para sistemas cliente-servidor se basa en tres pilares:
1. **`async`/`await`** para no bloquear hilos mientras se espera la red o la base de datos.
2. **`try/catch` con excepciones específicas y filtros `when`** para distinguir tipos de fallo.
3. **`CancellationToken`** para implementar timeouts y cancelación graceful.

Estos patrones permiten que PizzeriaAPI responda adecuadamente ante fallos de red, errores de datos y conflictos de negocio, manteniendo la consistencia mediante transacciones, la máquina de estados y los logs de error.
