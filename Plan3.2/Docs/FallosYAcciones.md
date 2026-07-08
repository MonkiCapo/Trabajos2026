# Clasificación de Fallos y Acciones Correctivas

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Introducción

En un sistema distribuido, los fallos son **la norma, no la excepción**. Al estar compuesto por múltiples procesos que se comunican por red, cada interacción está expuesta a distintos tipos de imponderables. Este documento clasifica los fallos identificados en PizzeriaAPI y define las acciones correctivas para cada caso.

---

## 2. Clasificación de Fallos

### 2.1 Fallos de Red

| Tipo | Descripción | Ejemplo en el sistema |
|------|-------------|----------------------|
| **Timeout de conexión** | El socket TCP no logra establecer conexión dentro del tiempo límite. | API intenta conectar con Cocina pero el servidor no responde. |
| **Conexión rechazada** | El host remoto rechaza activamente la conexión (puerto cerrado, proceso caído). | Servicio de Cocina no está ejecutándose. |
| **Pérdida de mensaje** | Un mensaje enviado por socket nunca llega a destino. | Notificación de "Pedido Preparado" se pierde en la red. |
| **Conexión interrumpida** | La conexión se cierra abruptamente durante la comunicación. | Cable de red cortado mientras Reparto notifica la entrega. |

### 2.2 Fallos de Proceso/Servicio

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Servicio no disponible** | El proceso remoto no está corriendo o falló al iniciar. | Cocina nunca se levantó. |
| **Crash durante ejecución** | El servicio se cae inesperadamente mientras procesa. | Cocina falla a mitad de la preparación simulada. |
| **Estado inconsistente** | La información del pedido difiere entre servicios. | API cree que el pedido está "EnPreparacion" pero Cocina lo perdió. |

### 2.3 Fallos de Datos

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Datos inválidos** | El payload recibido no cumple el contrato esperado. | Cliente envía un JSON con campos faltantes. |
| **Duplicación de mensajes** | El mismo mensaje llega más de una vez. | Cocina envía "PedidoPreparado" dos veces por un reintento. |

---

## 3. Acciones Correctivas

### 3.1 Estrategias por tipo de fallo

| Fallo | Acción | Implementación en el sistema |
|-------|--------|------------------------------|
| **Timeout de conexión** | Cancelar pedido + informar al cliente | Bloque `try/catch` al conectar socket; si expira el timeout, se cambia Estado a `Cancelado` y se responde HTTP 503. |
| **Conexión rechazada** | Cancelar pedido + reintento limitado | Capturar `SocketException`, registrar log, cancelar pedido. Opcional: reintentar 1 vez antes de cancelar. |
| **Pérdida de mensaje / socket caído** | Ninguna (el pedido queda en el último estado conocido) | El sistema asume pérdida del mensaje; el administrador debe intervenir. En una versión futura se podría implementar reintento. |
| **Servicio no disponible** | Degradación graceful | El endpoint POST responde 503 en lugar de 500, indicando que el recurso no está disponible. |
| **Datos inválidos** | Rechazar petición con 400 Bad Request | Validación de esquema al recibir el POST, antes de cualquier operación de red. |
| **Duplicación de mensajes** | Idempotencia parcial | Se valida el estado actual del pedido; si ya está en un estado posterior al evento recibido, se ignora. |

### 3.2 Mecanismos transversales

| Mecanismo | Propósito | Detalle |
|-----------|-----------|---------|
| **Logging estructurado** | Registrar todos los eventos de red y cambios de estado para trazabilidad. | Cada `try/catch` escribe en un log con timestamp, tipo de error y pedidoId. |
| **Timeouts explícitos** | Evitar que el sistema quede bloqueado esperando una respuesta. | Toda conexión socket tiene un timeout configurable (ej: 5 segundos). |
| **Máquina de estados** | Garantizar que las transiciones de estado sean predecibles. | El enumerador `EstadoPedido` solo permite transiciones definidas (ej: no se puede pasar de "EsperaConfirmacion" a "Entregado" sin pasar por "EnPreparacion"). |
| **Estado Cancelado** | Absorber fallos de red como un estado válido del ciclo de vida. | Si la cocina falla, el pedido transiciona a `Cancelado` en lugar de quedar en un estado inconsistente. |

---

## 4. Matriz de Decisión ante Fallos

```
¿Se pudo conectar a Cocina?
├── Sí → Continuar flujo normal
└── No → ¿Hubo timeout?
        ├── Sí → Cancelar pedido → 503
        └── No → ¿Conexión rechazada?
                ├── Sí → ¿Reintentar?
                │       ├── Sí (1er intento) → Reintentar conexión
                │       └── No (2do intento) → Cancelar pedido → 503
                └── No → Error genérico → 500 Internal Server Error
```

---

## 5. Conclusión

El sistema PizzeriaAPI maneja los fallos distribuidos mediante una combinación de:
- **Timeouts** para evitar bloqueos perpetuos.
- **Máquina de estados** que incluye `Cancelado` como estado terminal de error.
- **Respuestas HTTP semánticas** (201, 400, 503) que permiten al cliente reaccionar adecuadamente.
- **Logging** para diagnóstico post-mortem.

Para una versión productiva, se recomendaría agregar **reintentos con backoff**, **circuit breaker** y **colas de mensajes** (RabbitMQ) para garantizar la entrega de eventos.
