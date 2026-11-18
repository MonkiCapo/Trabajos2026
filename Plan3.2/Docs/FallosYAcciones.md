# Clasificación de Fallos y Acciones Correctivas

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Introducción

En un sistema cliente-servidor, los fallos son **la norma, no la excepción**. Cada interacción HTTP o de base de datos está expuesta a imponderables: caídas de servicio, datos mal formados, conflictos de estado. Este documento clasifica los fallos identificados en PizzeriaAPI y define las acciones correctivas para cada caso.

---

## 2. Clasificación de Fallos

### 2.1 Fallos de Red

| Tipo | Descripción | Ejemplo en el sistema |
|------|-------------|----------------------|
| **Timeout de conexión** | La petición HTTP no obtiene respuesta dentro del tiempo límite. | La API está sobrecargada y el cliente espera indefinidamente. |
| **Conexión rechazada** | El host remoto rechaza activamente la conexión (proceso caído, puerto cerrado). | `HttpRequestException` al llamar a la API cuando no está corriendo. |
| **Respuesta inesperada** | El servidor responde con un código no previsto por el cliente. | La Consola.Cliente recibe un 500 ante un error interno. |

### 2.2 Fallos de Proceso/Servicio

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Servicio no disponible** | La API no está corriendo o falló al iniciar. | La consola del cliente no puede registrar ni pedir. |
| **Caída de la base de datos** | MySQL se detiene o se pierde la conectividad. | `MySqlException` al abrir la conexión; la API responde 500. |
| **Estado inconsistente** | La información difiere entre capas. | Stock descontado sin pedido persistido (se evita con transacciones). |

### 2.3 Fallos de Datos

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Datos inválidos** | El payload recibido no cumple el contrato esperado. | JSON con email vacío o pedido sin ítems. |
| **Datos duplicados** | Un valor único se intenta registrar dos veces. | Segundo cliente con el mismo email; pizza con nombre repetido. |

### 2.4 Fallos de Reglas de Negocio

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Recurso inexistente** | Se referencia una entidad que no está en la base. | Pedido, cliente o pizza con id inexistente. |
| **Transición de estado inválida** | Se intenta saltar pasos del ciclo de vida. | Pasar de `EsperaConfirmacion` a `Entregado`. |
| **Inventario insuficiente** | La cantidad pedida supera el stock disponible. | Pedir 999 unidades de una pizza con stock 10. |

---

## 3. Acciones Correctivas

### 3.1 Estrategias por tipo de fallo

| Fallo | Acción | Implementación en el sistema |
|-------|--------|------------------------------|
| **Conexión rechazada / API caída** | Informar al usuario sin romper la app | La Consola.Cliente captura `HttpRequestException` y muestra un mensaje claro ("No se pudo conectar a la API"). |
| **Timeout HTTP** | Establecer límites y cancelar | `HttpClient` con `Timeout` configurado; `CancellationToken` en toda la cadena. |
| **Caída de MySQL** | Falla controlada con log | El middleware captura `MySqlException` y responde 500 registrando el detalle; no se corrompen datos por transacciones. |
| **Datos inválidos** | Rechazar con 400 | FluentValidation valida el request antes de tocar la base; `ArgumentException` de reglas de negocio también mapea a 400 con detalle. |
| **Datos duplicados** | Rechazar con 409 | El middleware traduce el error 1062 de MySQL a 409 Conflict con mensaje descriptivo. |
| **Recurso inexistente** | Responder 404 | `PedidoNoEncontradoException` y búsquedas sin resultado mapean a 404 Not Found. |
| **Transición inválida** | Rechazar con 409 sin tocar la base | `TransicionInvalidaException` con `estadoActual` y `estadoSolicitado`. |
| **Inventario insuficiente** | Rollback total + 400 | El `UPDATE` condicional de stock no afecta filas → se lanza error → `RollbackAsync` revierte pedido e ítems. |

### 3.2 Mecanismos transversales

| Mecanismo | Propósito | Detalle |
|-----------|-----------|---------|
| **Middleware global de errores** | Traducción uniforme excepción → HTTP | `ExceptionHandlingMiddleware` mapea dominio → 404/409, validación → 400, inesperado → 500 con log. |
| **Transacciones** | Atomicidad de escrituras | Creación de pedido (stock + pedido + ítems + historial) y cambios de estado se confirman o revierten como unidad. |
| **Máquina de estados** | Transiciones predecibles | Solo se permite avanzar un estado por vez y en orden; repetir el estado actual es idempotente. |
| **Stock atómico** | Evitar sobreventa | `UPDATE ... WHERE stock >= @Cantidad` evita condiciones de carrera entre pedidos concurrentes. |
| **Logging estructurado** | Trazabilidad post-mortem | `ILogger<T>` con niveles y placeholders en servicios y middleware. |
| **CancellationToken** | Cancelación graceful | Toda operación asíncrona (HTTP y BD) propaga el token del request. |

---

## 4. Matriz de Decisión ante Fallos

```
¿La API está disponible?
├── No → Consola muestra "No se pudo conectar a la API" (reintento manual)
└── Sí → ¿El request pasó la validación?
        ├── No → 400 Bad Request (FluentValidation / regla de negocio)
        └── Sí → ¿El recurso existe?
                ├── No → 404 Not Found
                └── Sí → ¿Hay conflicto de estado?
                        ├── Sí → 409 Conflict (duplicado, transición inválida, FK)
                        └── No → Operación exitosa (200/201)
                                └── Si algo inesperado falla → 500 + log
```

---

## 5. Conclusión

El sistema PizzeriaAPI maneja los fallos mediante una combinación de:
- **Respuestas HTTP semánticas** (400, 404, 409, 500) que permiten al cliente reaccionar adecuadamente.
- **Transacciones y stock atómico** para evitar estados inconsistentes o sobreventa.
- **Máquina de estados estricta e idempotente** como garantía del ciclo de vida.
- **Middleware centralizado** para que ningún error escape sin una respuesta estructurada.
- **Logging** para diagnóstico post-mortem.

Para una versión productiva, se recomendaría agregar **reintentos con backoff** en el cliente HTTP y **rate limiting** en la API.
