# Guía para la Defensa del Trabajo — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Estructura de la Presentación (10-15 minutos)

### 1.1 Arquitectura del Sistema (3 min)
- Mostrar el diagrama de arquitectura (ver `ArquitecturaDistribuida.md`)
- Explicar los componentes:
  - **Api.Pizzeria**: Backend central (Minimal API ASP.NET Core)
  - **Consola.Cliente**: Interfaz del usuario (cliente HTTP)
  - **Swagger UI**: panel del administrador (usa los mismos endpoints)
  - **MySQL**: persistencia (acceso con Dapper + MySqlConnector)
- Explicar el protocolo de comunicación: **HTTP REST con JSON** entre todos los componentes.

### 1.2 Demo en Vivo (5 min)
Ejecutar el flujo completo:
1. Iniciar MySQL y la API (en ese orden)
2. Registrar un cliente con Swagger o la consola
3. Ver catálogo de pizzas (con stock y disponibilidad)
4. Hacer un pedido (queda en `EsperaConfirmacion` y descuenta stock)
5. Avanzar el estado como administrador: `PUT /api/pedidos/{id}/estado` (EnPreparacion → EnViaje → Entregado)
6. Mostrar el polling de la Consola.Cliente reflejando los cambios
7. Provocar un error en vivo: transición inválida → 409, email duplicado → 409, stock insuficiente → 400

### 1.3 Desafíos y Soluciones (3-5 min)
Ver sección 2 de esta guía.

---

## 2. Desafíos Encontrados y Cómo se Solucionaron

### Desafío 1: Máquina de Estados con Validación Estricta

**El problema:**
El pedido debe pasar obligatoriamente por `EsperaConfirmacion → EnPreparacion → EnViaje → Entregado`. Sin validación, un administrador (o un bug) podría saltear estados y dejar el sistema inconsistente.

**La solución:**
Implementamos un diccionario de transiciones válidas en `PedidoService`:

```csharp
private static readonly Dictionary<EstadoPedido, EstadoPedido> TransicionesValidas = new()
{
    [EstadoPedido.EsperaConfirmacion] = EstadoPedido.EnPreparacion,
    [EstadoPedido.EnPreparacion] = EstadoPedido.EnViaje,
    [EstadoPedido.EnViaje] = EstadoPedido.Entregado
};
```

- Solo se permite avanzar **un estado por vez** y en orden.
- Una transición inválida lanza `TransicionInvalidaException` → **409 Conflict** con `estadoActual` y `estadoSolicitado`.
- **Idempotencia:** repetir el estado actual responde 200 sin efectos colaterales.
- Cada cambio se audita en `HISTORIAL_ESTADO_PEDIDO` dentro de una transacción.

**Conceptos clave que aprendí:**
- Modelar el ciclo de vida como máquina de estados finita.
- Validar transiciones en la capa de servicio, no solo en la UI.
- Diferenciar errores de negocio (409) de errores de datos (400).

### Desafío 2: Stock Atómico con Transacciones

**El problema:**
Al crear un pedido hay que escribir en varias tablas (PEDIDO, ITEM_PEDIDO, HISTORIAL) y, además, descontar el stock de cada pizza. Si algo falla a mitad de camino, no puede quedar stock descontado sin pedido (ni pedido sin stock descontado). También hay que evitar vender stock que no existe (condición de carrera entre dos pedidos simultáneos).

**La solución:**
Todo ocurre dentro de **una única transacción** con descuento atómico:

```sql
UPDATE PIZZA
SET stock = stock - @Cantidad
WHERE id = @Id AND stock >= @Cantidad;
```

- Si `stock < cantidad`, la condición del `WHERE` no se cumple, se afectan **0 filas** y se lanza un error → **400** y `Rollback`.
- El `UPDATE` condicional es atómico: dos pedidos concurrentes no pueden llevarse el mismo stock.
- Cualquier fallo posterior revierte también el descuento de stock ya aplicado.

**Conceptos clave:**
- `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`.
- El descuento condicional evita leer y luego escribir (read-modify-write no atómico).
- La transacción abarca persistencia + inventario como unidad de trabajo.

### Desafío 3: Uso del Logger (Microsoft.Extensions.Logging)

**El problema:**
Al principio intentaba usar `Console.WriteLine()` directamente, pero no me permitía estructurar los mensajes. Necesitaba:
- Logging estructurado con parámetros
- Diferentes niveles (Information, Warning, Error)
- Que fuera configurable desde `appsettings.json`

**La solución:**
Aprendí a inyectar `ILogger<T>` en el constructor de cada servicio:

```csharp
public PedidoService(IAdo ado, IPedidoRepositorio repositorio,
    IClienteRepositorio clientes, IPizzaRepositorio pizzas,
    ILogger<PedidoService> logger)
{
    _logger = logger;
}
```

Y usar las plantillas de logging:

```csharp
_logger.LogInformation("[PEDIDOSERVICE] Pedido {Id} creado con {Cantidad} items.", pedidoId, items.Count);
_logger.LogWarning("[PEDIDOSERVICE] Stock insuficiente para la pizza {PizzaId}.", pizza.Id);
_logger.LogError(ex, "[PEDIDOSERVICE] Error al crear el pedido; se revierte la transaccion.");
```

**Conceptos clave que aprendí:**
- Diferencia entre `LogInformation`, `LogWarning`, `LogError`
- Las plantillas con `{Placeholder}` son más eficientes que concatenar strings
- `LogError(ex, ...)` incluye el stack trace completo
- El middleware de errores loguea los 500 para diagnóstico post-mortem

### Desafío 4: Middleware Global de Errores

**El problema:**
Al principio cada endpoint tenía su propio `try/catch` duplicando la lógica de "traducir excepción → código HTTP". Era código repetido y fácil de olvidar.

**La solución:**
Centralizamos toda la traducción en `ExceptionHandlingMiddleware`:

```csharp
catch (PedidoNoEncontradoException)            → 404
catch (TransicionInvalidaException)            → 409 (con estadoActual/estadoSolicitado)
catch (MySqlException ex) when (ex.Number == 1062) → 409 (registro duplicado)
catch (ArgumentException)                      → 400 { error, detalles }
catch (Exception ex)                           → 500 (se registra en el log)
```

**Conceptos clave:**
- Los endpoints quedan limpios: solo lanzan excepciones de dominio.
- El middleware responde JSON uniforme en toda la API.
- Los filtros `when` permiten distinguir errores de MySQL sin anidar catch.

---

## 3. Preguntas Frecuentes de la Evaluación

### "¿Por qué el cambio de estado es manual (PUT) y no automático?"

**Respuesta:** La consigna pide un ciclo de vida con estados controlados (`EsperaConfirmacion → EnPreparacion → EnViaje → Entregado`). Con el endpoint `PUT /api/pedidos/{id}/estado`, el administrador simula cada avance real (cocina, reparto) y la API garantiza que las transiciones sean válidas. Esto simplifica el sistema a un modelo cliente-servidor HTTP puro, sin procesos extra, manteniendo la máquina de estados como garantía de consistencia.

### "¿Qué pasa si alguien intenta saltear un estado?"

**Respuesta:** La API lo rechaza con **409 Conflict** y devuelve el detalle (`estadoActual`, `estadoSolicitado`). La base de datos no se modifica. Enviar el estado actual es idempotente: responde 200 sin duplicar historial.

### "¿Cómo garantizan que no se venda stock que no existe?"

**Respuesta:** El descuento es un `UPDATE ... WHERE id = @Id AND stock >= @Cantidad` dentro de la misma transacción que crea el pedido. Si no alcanza el stock, no se actualiza ninguna fila, se lanza un error 400 y el `Rollback` deshace todo lo demás. Al ser un solo `UPDATE` condicional, es seguro ante pedidos concurrentes.

### "¿Por qué devuelven 409 y no 400 o 500?"

**Respuesta:** 409 Conflict es el código semántico para "la petición no se puede completar por el estado actual del recurso": email duplicado, nombre de pizza duplicado, transición inválida o eliminación de una pizza con pedidos. 400 queda para datos mal formados (validación) y 500 para errores inesperados. La distinción permite al cliente reaccionar distinto en cada caso.

### "¿Cómo funciona el patrón de diseño de las interfaces IAdo e IPedidoService?"

**Respuesta:** Aplicamos el **Principio de Inversión de Dependencias (D de SOLID)**:
- `IAdo` define el contrato de acceso a datos sin depender de MySQL
- `Ado` devuelve `DbConnection`, la abstracción de ADO.NET
- `PedidoService` depende de interfaces de repositorios, no de implementaciones Dapper
- Esto permite hacer mocking para tests y cambiar el motor de datos sin modificar el servicio

### "¿Por qué usan Dapper y no Entity Framework?"

**Respuesta:** Dapper es un micro-ORM: mapea resultados a objetos con SQL escrito a mano. Para un proyecto escolar nos da control total sobre las queries (como el `UPDATE` atómico de stock o el multi-insert de ítems) y nos obliga a entender el SQL que ejecutamos, sin la magia del tracking de EF.

### "¿Por qué el Logging es importante en un sistema cliente-servidor?"

**Respuesta:** Porque los errores de red y de datos no se ven en la pantalla del usuario. El logger nos permite:
1. **Identificar el origen:** `[PEDIDOSERVICE]`, `[API]`, `[MIDDLEWARE]`
2. **Nivel de severidad:** Information, Warning, Error
3. **Parámetros estructurados:** `{Id}`, `{PizzaId}` (más eficiente que concatenar)
4. **Stack traces:** con `_logger.LogError(ex, ...)` se guarda la excepción completa
5. **Configuración:** se puede cambiar el nivel desde `appsettings.json` sin tocar código

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
