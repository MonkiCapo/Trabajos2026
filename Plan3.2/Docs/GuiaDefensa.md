# Guía para la Defensa del Trabajo — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Estructura de la Presentación (10-15 minutos)

### 1.1 Arquitectura del Sistema (3 min)
- Mostrar el diagrama de arquitectura (ver `ArquitecturaDistribuida.md`)
- Explicar los 4 procesos independientes:
  - **Api.Pizzeria**: Backend central (Minimal API + Socket Server)
  - **Consola.Cliente**: Interfaz del usuario (HTTP)
  - **Consola.Cocina**: Servicio de cocina (TCP Socket)
  - **Consola.Reparto**: Servicio de reparto (TCP Socket)
- Explicar por qué se usan **dos protocolos distintos**:
  - HTTP para Cliente↔Backend (síncrono, request/response)
  - TCP Socket para Backend↔Cocina/Reparto (asíncrono, persistente)

### 1.2 Demo en Vivo (5 min)
Ejecutar el flujo completo:
1. Iniciar MySQL, Cocina, Reparto, API, Cliente (en ese orden)
2. Registrar un cliente
3. Ver catálogo de pizzas
4. Hacer un pedido
5. Mostrar cómo el estado cambia en tiempo real
6. Consultar el pedido por ID

### 1.3 Desafíos y Soluciones (3-5 min)
Ver sección 2 de esta guía.

---

## 2. Desafíos Encontrados y Cómo se Solucionaron

### Desafío 1: Comunicación Asíncrona con TCP Sockets

**El problema:**
La comunicación HTTP es síncrona (el cliente hace una pregunta y recibe una respuesta). Pero la cocina y el reparto son procesos independientes que trabajan a su ritmo. Necesitábamos que el backend pudiera enviar un pedido a la cocina y seguir funcionando sin bloquearse esperando la respuesta.

**La solución:**
Implementamos un **servidor TCP con `TcpListener`** en `SocketServer.cs` que:
- Escucha conexiones entrantes en el puerto 7000
- Acepta múltiples clientes simultáneamente
- Usa `BackgroundService` para ejecutarse como servicio hospedado
- Implementa un protocolo ACK con `TaskCompletionSource` para confirmar recepción
- Maneja timeout de 5 segundos con `CancellationTokenSource`

**Conceptos clave que aprendí:**
- `TcpListener.AcceptTcpClientAsync()` para aceptar conexiones
- `StreamReader/StreamWriter` para leer/escribir líneas JSON
- `ConcurrentDictionary<int, TaskCompletionSource<bool>>` para mapear pedidos pendientes de ACK
- `Task.WhenAny(tcs.Task, Task.Delay(5000))` para implementar timeout

### Desafío 2: Uso del Logger (Microsoft.Extensions.Logging)

**El problema:**
Al principio no sabía cómo usar el sistema de logging de .NET. Intentaba usar `Console.WriteLine()` directamente, pero no me permitía "estructurarlo". Haciendo que necesite:
- Logging estructurado con parámetros
- Diferentes niveles (Information, Warning, Error)
- Que fuera configurable desde `appsettings.json`

**La solución:**
Aprendí a inyectar `ILogger<T>` en el constructor de cada servicio:
```csharp
public PedidoService(IConfiguration configuration, ISocketServer socketServer, ILogger<PedidoService> logger)
{
    _logger = logger;
}
```

Y usar las plantillas de logging:
```csharp
_logger.LogInformation("[PEDIDOSERVICE] Enviando pedido {Id} a Cocina...", pedido.Id);
_logger.LogWarning("[PEDIDOSERVICE] ACK fallido para pedido {Id}.", pedido.Id);
_logger.LogError(ex, "[PEDIDOSERVICE] Error al crear pedido en la base de datos.");
```

**Conceptos clave que aprendí:**
- Diferencia entre `LogInformation`, `LogWarning`, `LogError`
- Las plantillas de logging con `{Placeholder}` son más eficientes que concatenar strings
- El primer parámetro es la plantilla, los siguientes son los valores
- El ` ex ` como tercer parámetro en `LogError` incluye el stack trace completo

### Desafío 3: Transacciones de Base de Datos

**El problema:**
Al crear un pedido, se deben insertar registros en 3 tablas (PEDIDO, ITEM_PEDIDO, HISTORIAL_ESTADO_PEDIDO). Si una inserción falla, las otras deben deshacerse.

**La solución:**
Usamos transacciones de MySQL con Dapper:
```csharp
using var transaction = connection.BeginTransaction();
try {
    // ... inserciones ...
    transaction.Commit();
} catch {
    transaction.Rollback();
    throw;
}
```

**Conceptos clave:**
- La interacción por socket se hace **después** del commit, no dentro de la transacción
- Esto evita que un timeout de red bloquee la base de datos

### Desafío 4: Handshake y Identificación de Clientes Socket

**El problema:**
El servidor socket no sabe quién es quien. Un cliente cualquiera puede conectarse. Necesitábamos un protocolo para que cada cliente se identifique.

**La solución:**
Implementamos un handshake simple:
1. El cliente se conecta y envía: `{ "accion": "identificar", "tipo": "cocina" }`
2. El servidor lo registra como `_cocinaClient` o `_repartoClient`
3. Si el tipo es desconocido, se cierra la conexión

---

## 3. Preguntas Frecuentes de la Evaluación

### "¿Por qué usaron TCP Sockets en vez de WebSockets?"

**Respuesta:** Los WebSockets son ideales para comunicación cliente-servidor en navegadores. En nuestro caso, los servicios de Cocina y Reparto son procesos de consola C#, no navegadores. TCP Sockets nos dan control total sobre el protocolo y la conexión persistente, sin la sobrecarga del handshake HTTP de WebSockets. Además, el protocolo es más simple: líneas JSON delimitadas por `\n`.

### "¿Qué pasa si la cocina se desconecta momentáneamente?"

**Respuesta:** El sistema maneja tres escenarios:
1. **Cocina no conectada al enviar pedido:** Se lanza `SocketException` → El pedido se cancela → HTTP 503
2. **Cocina conectada pero no responde ACK en 5s:** Timeout → Pedido cancelado → HTTP 503
3. **Cocina se desconecta después de confirmar ACK:** El pedido sigue en `EnPreparacion` hasta que la cocina reconecte y envíe el evento `pedido_preparado`

### "¿Por qué usan transacciones si el socket es después?"

**Respuesta:** La transacción garantiza que el pedido quede **consistente en la base de datos** antes de intentar comunicarse por socket. Si la transacción falla, no hay datos parciales. Si el socket falla, el pedido queda en `EsperaConfirmacion` y se cancela. La separación entre persistencia y comunicación de red es clave en sistemas distribuidos.

### "¿Cómo funciona el patrón de diseño de las interfaces ISocketServer e IPedidoService?"

**Respuesta:** Aplicamos el **Principio de Inversión de Dependencias (D de SOLID)**:
- `ISocketServer` define el contrato sin depender de la implementación
- `SocketServer` implementa la interfaz
- `PedidoService` depende de `ISocketServer`, no de `SocketServer`
- Esto permite hacer mocking para tests y cambiar la implementación sin modificar el servicio

### "¿Por qué el Logging es importante en un sistema distribuido?"

**Respuesta:** En un sistema con múltiples procesos, `Console.WriteLine()` no es suficiente. El logger nos permite:
1. **Identificar el origen:** `[PEDIDOSERVICE]`, `[SOCKETSERVER]`, `[API]`
2. **Nivel de severidad:** Information, Warning, Error
3. **Parámetros estructurados:** `{Id}`, `{Type}` (más eficiente que concatenar strings)
4. **Stack traces:** Cuando usamos `_logger.LogError(ex, ...)`, se guarda la excepción completa
5. **Configuración:** Se puede cambiar el nivel de logging sin modificar código

---

## 4. Documentos de Referencia

| Documento | Contenido |
|-----------|-----------|
| `Relevamiento.md` | Diagramas de clases, ER y secuencia |
| `ArquitecturaDistribuida.md` | Diagrama de arquitectura y protocolos |
| `CasosDeUso.md` | 7 casos de uso documentados |
| `FlujoFuncional.md` | Flujo completo paso a paso |
| `PreRequisitos.md` | Requisitos y guía de instalación |
| `ExploracionAPI.md` | Guía de uso de la API |
| `GuiaSwagger.md` | Cómo usar Swagger UI |

---