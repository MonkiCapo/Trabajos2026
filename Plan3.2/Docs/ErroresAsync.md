# Análisis de Manejo de Errores Asincrónicos en C#

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Introducción

En un sistema distribuido como PizzeriaAPI, las operaciones de red (HTTP, Sockets TCP) son inherentemente asíncronas. C# proporciona `async`/`await` para trabajar con operaciones no bloqueantes, pero el manejo de errores en este contexto tiene particularidades que deben tenerse en cuenta.

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
            "http://localhost:5000/api/pedidos", content);

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

### 2.2 Conexión por Socket TCP (Backend → Cocina)

```csharp
public async Task<bool> EnviarPedidoACocinaAsync(Pedido pedido, int timeoutSegundos = 5)
{
    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    try
    {
        var connectTask = socket.ConnectAsync("localhost", 7000);
        if (await Task.WhenAny(connectTask, Task.Delay(timeoutSegundos * 1000)) == connectTask)
        {
            await connectTask; // Propagar excepción si la conexión falló

            string mensaje = JsonSerializer.Serialize(new
            {
                accion = "nuevo_pedido",
                pedidoId = pedido.Id,
                items = pedido.Items
            });

            byte[] data = Encoding.UTF8.GetBytes(mensaje + "\n");
            await socket.SendAsync(data, SocketFlags.None);

            // Esperar ACK
            byte[] buffer = new byte[1024];
            int recibidos = await socket.ReceiveAsync(buffer, SocketFlags.None);
            string respuesta = Encoding.UTF8.GetString(buffer, 0, recibidos);

            return respuesta.Contains("ack");
        }
        else
        {
            Console.WriteLine($"[TIMEOUT] Cocina no respondio en {timeoutSegundos}s");
            return false;
        }
    }
    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
    {
        Console.WriteLine("[ERROR] Conexion rechazada por Cocina");
        return false;
    }
    catch (SocketException ex)
    {
        Console.WriteLine($"[ERROR] Socket: {ex.SocketErrorCode} - {ex.Message}");
        return false;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("[ERROR] Operacion cancelada");
        return false;
    }
}
```

**Análisis de buenas prácticas:**
- ✅ Uso de `Task.WhenAny` para implementar timeout manual sobre `ConnectAsync`.
- ✅ Filtros de excepción (`when`) para tratar distintos errores de socket.
- ✅ `Socket` envuelto en `using` para liberar recursos.
- ❌ **Mejorable:** El timeout con `Task.Delay` deja una tareas huérfanas (no se cancelan). Versión mejorada abajo.

---

### 2.3 Versión mejorada con CancellationToken

```csharp
public async Task<bool> EnviarPedidoACocinaAsync(Pedido pedido, CancellationToken ct = default)
{
    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 7000), cts.Token);

        string mensaje = JsonSerializer.Serialize(new
        {
            accion = "nuevo_pedido",
            pedidoId = pedido.Id,
            items = pedido.Items
        });

        byte[] data = Encoding.UTF8.GetBytes(mensaje + "\n");
        await socket.SendAsync(data, SocketFlags.None, cts.Token);

        byte[] buffer = new byte[1024];
        int recibidos = await socket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
        string respuesta = Encoding.UTF8.GetString(buffer, 0, recibidos);

        return respuesta.Contains("ack");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("[TIMEOUT] Cocina no respondio a tiempo");
        return false;
    }
    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
    {
        Console.WriteLine("[ERROR] Cocina rechazo la conexion");
        return false;
    }
}
```

**Mejoras:**
- ✅ `CancellationTokenSource` con `CancelAfter()` reemplaza el `Task.WhenAny` manual.
- ✅ Se cancela correctamente la tarea de conexión si expira el plazo.
- ✅ Se usa `CreateLinkedTokenSource` para permitir cancelación externa.

---

## 3. Escenario: Fallo simulado de red

```csharp
// Simulación de fallo para pruebas
public static async Task SimularFalloDeRed()
{
    Console.WriteLine("Simulando corte de red...");

    try
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // Intentar conectar a un puerto que no existe
        await socket.ConnectAsync("localhost", 9999);
    }
    catch (SocketException ex)
    {
        Console.WriteLine($"Fallo capturado: {ex.SocketErrorCode}");
        Console.WriteLine($"Mensaje: {ex.Message}");

        switch (ex.SocketErrorCode)
        {
            case SocketError.ConnectionRefused:
                Console.WriteLine("Accion: Reintentar mas tarde");
                break;
            case SocketError.TimedOut:
                Console.WriteLine("Accion: Cancelar pedido");
                break;
            case SocketError.HostUnreachable:
                Console.WriteLine("Accion: Verificar conectividad de red");
                break;
            default:
                Console.WriteLine("Accion: Error desconocido, loguear y escalar");
                break;
        }
    }
}
```

---

## 4. Buenas prácticas resumidas

| Práctica | Descripción |
|----------|-------------|
| **Usar CancellationToken** | Siempre pasar `CancellationToken` a operaciones async de red para poder cancelarlas. |
| **Timeouts explícitos** | No confiar en timeouts por defecto; establecerlos siempre (`CancelAfter`, `Task.WhenAny`). |
| **Capturar excepciones específicas** | Preferir `SocketException`, `HttpRequestException`, `TaskCanceledException` sobre `Exception` genérico. |
| **Filtros de excepción (`when`)** | Permite switchear sobre `SocketErrorCode` sin anidar catch. |
| **Log antes de relanzar** | Registrar el error en el punto de captura antes de propagar hacia arriba. |
| **No mezclar sync con async** | Evitar `.Result` o `.Wait()`; usar `await` en toda la cadena. |
| **`using` en recursos IDisposable** | `Socket`, `HttpClient`, `CancellationTokenSource` deben liberarse. |

---

## 5. Conclusión

El manejo de errores asincrónicos en C# para sistemas distribuidos se basa en tres pilares:
1. **`async`/`await`** para no bloquear hilos mientras se espera la red.
2. **`try/catch` con excepciones específicas** para distinguir tipos de fallo.
3. **`CancellationToken`** para implementar timeouts y cancelación graceful.

Estos patrones permiten que PizzeriaAPI responda adecuadamente ante fallos de red, timeouts y servicios caídos, manteniendo la consistencia del sistema mediante la máquina de estados y los logs de error.
