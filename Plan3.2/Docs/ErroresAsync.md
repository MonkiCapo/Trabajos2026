# Análisis de Manejo de Errores Asincrónicos en C#

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Introducción

En PizzeriaAPI, las operaciones de red (HTTP) son inherentemente asíncronas. C# proporciona `async`/`await` para trabajar con operaciones no bloqueantes, pero el manejo de errores en este contexto tiene particularidades que deben tenerse en cuenta.

---

## 2. Patrón básico: async/await + try/catch

### 2.1 Llamada HTTP desde el cliente

```csharp
public async Task<PedidoResponse> CrearPedidoAsync(PedidoRequest request)
{
    using var httpClient = new HttpClient();
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        HttpResponseMessage response = await httpClient.PostAsync(
            "http://localhost:5183/api/pedidos", content);

        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PedidoResponse>(body);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"[ERROR] Fallo de red: {ex.Message}");
        throw new ServicioNoDisponibleException("No se pudo contactar al servidor");
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
- ✅ `HttpClient` se descarta correctamente con `using`.

---

### 2.2 Llamada HTTP con timeout explícito (CancellationToken)

```csharp
public async Task<Pedido> ConsultarPedidoAsync(int pedidoId, CancellationToken ct = default)
{
    using var httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5183")
    };

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(5));

    try
    {
        return await httpClient.GetFromJsonAsync<Pedido>($"/api/pedidos/{pedidoId}", cts.Token)
            ?? throw new RecursoNoEncontradoException($"Pedido {pedidoId} no encontrado");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("[TIMEOUT] El servidor no respondio a tiempo");
        throw new ServicioNoDisponibleException("El servidor no respondio a tiempo");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"[ERROR] Fallo de red: {ex.Message}");
        throw new ServicioNoDisponibleException("No se pudo contactar al servidor");
    }
}
```

**Mejoras:**
- ✅ `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter()` implementa timeout sobre cualquier operación HTTP.
- ✅ Se cancela correctamente la tarea si expira el plazo.
- ✅ El `HttpClient` se crea con `BaseAddress` para no repetir la URL.

---

## 3. Patrones en el servicio de dominio (PedidoService)

### 3.1 Crear pedido con transacción y propagación de errores

```csharp
public async Task<Pedido> CrearPedidoAsync(Pedido nuevoPedido)
{
    using var conexion = _ado.GetDbConnection();
    conexion.Open();
    using var transaction = conexion.BeginTransaction();

    try
    {
        // validar y persistir...
        transaction.Commit();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        _logger.LogError(ex, "Error al crear pedido.");
        throw;
    }

    return nuevoPedido;
}
```

**Análisis:**
- ✅ `using` / `using var` garantiza que la conexión y la transacción se liberen.
- ✅ `Rollback()` revierte la transacción ante cualquier error.
- ✅ Se registra el error y se **relanza** la excepción original para que el endpoint responda el status correcto (400/500).

---

## 4. Buenas prácticas resumidas

| Práctica | Descripción |
|----------|-------------|
| **Usar CancellationToken** | Siempre pasar `CancellationToken` a operaciones async de red para poder cancelarlas. |
| **Timeouts explícitos** | No confiar en timeouts por defecto; establecerlos siempre (`CancelAfter`, `Task.WhenAny`). |
| **Capturar excepciones específicas** | Preferir `HttpRequestException`, `TaskCanceledException`, `JsonException` sobre `Exception` genérico. |
| **Filtros de excepción (`when`)** | Permite tratar distintos casos sin anidar catch. |
| **Log antes de relanzar** | Registrar el error en el punto de captura antes de propagar hacia arriba. |
| **No mezclar sync con async** | Evitar `.Result` o `.Wait()`; usar `await` en toda la cadena. |
| **`using` en recursos IDisposable** | `HttpClient`, conexiones y transacciones deben liberarse. |

---

## 5. Conclusión

El manejo de errores asincrónicos en C# para una API REST se basa en tres pilares:
1. **`async`/`await`** para no bloquear hilos mientras se espera la red.
2. **`try/catch` con excepciones específicas** para distinguir tipos de fallo.
3. **`CancellationToken`** para implementar timeouts y cancelación graceful.

Estos patrones permiten que PizzeriaAPI responda adecuadamente ante fallos de red, timeouts y errores de datos, manteniendo la consistencia del sistema mediante la máquina de estados y el historial de cambios.