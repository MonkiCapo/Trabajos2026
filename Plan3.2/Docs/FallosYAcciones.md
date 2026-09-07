# Clasificación de Fallos y Acciones Correctivas

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Introducción

Al haber eliminado los sockets TCP, la comunicación del sistema se reduce a un único canal **HTTP** sobre la API. Esto simplifica el tratamiento de fallos: ya no hay que lidiar con conexiones persistentes, timeouts de socket entre servicios ni eventos que puedan perderse en la red. Los fallos restantes son los típicos de una API REST y de la base de datos.

---

## 2. Clasificación de Fallos

### 2.1 Fallos de Datos / Validación

| Tipo | Descripción | Ejemplo en el sistema |
|------|-------------|----------------------|
| **Datos inválidos** | El payload recibido no cumple el contrato esperado. | Cliente envía un JSON con campos faltantes o formatos incorrectos. |
| **Recurso inexistente** | El ID solicitado no existe en la BD. | GET o PATCH de un pedido que no existe. |
| **Conflictos de unicidad** | Se intenta duplicar un valor único. | Registrar dos clientes con el mismo email. |
| **Estado inválido** | El nuevo estado enviado no es un valor válido del enumerador. | PATCH con `estado: "Despachado"` (no existe). |

### 2.2 Fallos de Servicio/Proceso

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Base de datos caída** | MySQL no está disponible. | La API no puede persistir ni leer. |
| **Error inesperado en el servidor** | Excepción no controlada durante el procesamiento. | Error de lógica o de infraestructura. |

---

## 3. Acciones Correctivas

### 3.1 Estrategias por tipo de fallo

| Fallo | Acción | Implementación en el sistema |
|-------|--------|------------------------------|
| **Datos inválidos** | Rechazar petición con 400 Bad Request | Validación con FluentValidation antes de cualquier operación. |
| **Recurso inexistente** | Responder 404 Not Found | Endpoints verifican existencia antes de operar. |
| **Email duplicado** | Responder 400 con mensaje descriptivo | Se detecta el error 1062 de MySQL o se valida previamente. |
| **Pizza inexistente** | Responder 400 con mensaje descriptivo | `ArgumentException` → capturada en el endpoint. |
| **Base de datos caída** | Responder 500 con log | La conexión falla y se registra el error. |
| **Error inesperado** | Responder 500 con log estructurado | `catch (Exception)` en cada endpoint. |

### 3.2 Mecanismos transversales

| Mecanismo | Propósito | Detalle |
|-----------|-----------|---------|
| **Logging estructurado** | Registrar operaciones y errores para trazabilidad. | Cada `try/catch` escribe en un log con timestamp, tipo de error y pedidoId. |
| **Máquina de estados** | Garantizar que las transiciones de estado sean predecibles. | El enumerador `EstadoPedido` centraliza los estados; el historial registra cada cambio. |
| **Transacciones SQL** | Garantizar consistencia entre pedido, items e historial. | Crear pedido y transicionar estado se ejecutan en una transacción con commit/rollback. |
| **Estado Cancelado** | Absorber anulaciones como un estado válido del ciclo de vida. | El administrador puede cancelar un pedido explícitamente vía PATCH. |

---

## 4. Matriz de Decisión ante Fallos

```
¿El pedido existe?
├── No → 404 Not Found
└── Sí → ¿El estado enviado es válido?
        ├── No → 400 Bad Request (FluentValidation)
        └── Sí → ¿La transacción SQL se completó?
                ├── Sí → 200 OK (estado actualizado)
                └── No → 500 Internal Server Error
```

---

## 5. Conclusión

El sistema PizzeriaAPI maneja los fallos mediante una combinación de:
- **Validación de entrada** (FluentValidation) que rechaza datos inválidos tempranamente.
- **Códigos HTTP semánticos** que permiten al cliente reaccionar adecuadamente.
- **Transacciones SQL** que mantienen la consistencia de la base de datos.
- **Máquina de estados + historial** que garantiza trazabilidad total de cada pedido.
- **Logging** para diagnóstico post-mortem.

Al simplificar la arquitectura (sin sockets), la superficie de fallos posibles se reduce y el sistema es más fácil de operar y depurar.