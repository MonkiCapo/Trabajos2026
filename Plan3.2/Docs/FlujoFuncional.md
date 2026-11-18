# Flujo Funcional del Sistema — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Descripción General

El sistema es una aplicación **cliente-servidor** de gestión de pedidos de pizzería compuesta por dos procesos independientes que se comunican mediante **HTTP REST (JSON)**:

- **Consola.Cliente:** interfaz de usuario final (registro, pedidos, seguimiento).
- **Api.Pizzeria:** backend central (Minimal API) con validación, reglas de negocio y persistencia en MySQL.

---

## 2. Arquitectura de Procesos

```
┌─────────────────────┐
│   Consola.Cliente   │  ← Usuario final
│   (App de consola)  │
└─────────┬───────────┘
          │ HTTP (JSON)
          ▼
┌─────────────────────┐
│   Api.Pizzeria      │  ← Backend central (también usado por el
│   (Minimal API)     │    administrador vía Swagger UI)
│                     │
│   - Endpoints REST  │
│   - FluentValidation│
│   - Middleware      │
└─────────┬───────────┘
          │ SQL (Dapper + MySqlConnector)
          ▼
┌─────────────────────┐
│   MySQL             │
│   (5to_Pizzeria)    │
└─────────────────────┘
```

---

## 3. Flujo Completo de un Pedido

### Fase 1: Registro del Cliente

```
1. El cliente inicia la Consola.Cliente
2. Selecciona la opción "Registrar" e ingresa sus datos
3. La app envía POST /api/clientes
4. El backend valida con FluentValidation y verifica unicidad de email
5. El cliente queda registrado y recibe su id
```

### Fase 2: Creación del Pedido

```
┌─────────────────────────────────────────────────────────────────────┐
│                    FLUJO DE CREACIÓN DE PEDIDO                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. CLIENTE envía POST /api/pedidos                                │
│     └─ Body: { clienteEmail, items[] }                             │
│                                                                     │
│  2. BACKEND valida:                                                 │
│     ├─ FluentValidation (al menos un item, cantidades positivas)   │
│     └─ Resuelve cliente por email y pizzas por nombre (IN)         │
│                                                                     │
│  3. BACKEND abre TRANSACCIÓN y:                                    │
│     ├─ Verifica que cada pizza esté disponible                     │
│     ├─ Descuenta stock atómicamente:                               │
│     │    UPDATE PIZZA SET stock = stock - @Cantidad                │
│     │    WHERE id = @Id AND stock >= @Cantidad                     │
│     ├─ INSERT PEDIDO (estado: EsperaConfirmacion)                  │
│     ├─ INSERT ITEM_PEDIDO (precio unitario congelado)              │
│     └─ INSERT HISTORIAL_ESTADO_PEDIDO                              │
│                                                                     │
│  4. COMMIT → Responde HTTP 201:                                    │
│     └─ { pedidoId, estado, total, fechaCreacion }                  │
│                                                                     │
│  En cualquier fallo: ROLLBACK → HTTP 400 con detalle               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Fase 3: Ciclo de Vida del Pedido (Administrador)

```
1. El pedido nace en EsperaConfirmacion
2. El administrador avanza el estado con PUT /api/pedidos/{id}/estado:
   - EnPreparacion: simula la aceptación en cocina
   - EnViaje:       simula la salida a reparto
   - Entregado:     simula la entrega al cliente
3. Cada cambio valida la transición y se registra en el historial
```

### Fase 4: Consulta y Seguimiento (Cliente)

```
- El cliente consulta el estado vía GET /api/pedidos/{id}
- La Consola.Cliente implementa polling cada 2 segundos
- Muestra los cambios de estado con colores:
  • Amarillo = EsperaConfirmacion
  • Azul     = EnPreparacion
  • Magenta  = EnViaje
  • Verde    = Entregado
```

---

## 4. Máquina de Estados del Pedido

```
EsperaConfirmacion ──► EnPreparacion ──► EnViaje ──► Entregado
```

| Transición | Disparador (Administrador) |
|-----------|---------------------------|
| `EsperaConfirmacion` → `EnPreparacion` | `PUT /api/pedidos/{id}/estado` con `EnPreparacion` |
| `EnPreparacion` → `EnViaje` | `PUT /api/pedidos/{id}/estado` con `EnViaje` |
| `EnViaje` → `Entregado` | `PUT /api/pedidos/{id}/estado` con `Entregado` |

- Solo se permite avanzar **un estado por vez** y en orden.
- Enviar el estado actual es **idempotente** (responde 200 sin cambios).
- Cualquier salto fuera de orden responde **409 Conflict** sin tocar la base.

---

## 5. Endpoints de la API REST

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/clientes` | Registrar nuevo cliente |
| `GET` | `/api/clientes/{id}` | Obtener cliente por ID |
| `GET` | `/api/clientes/email/{email}` | Obtener cliente por email |
| `GET` | `/api/pizzas` | Catálogo de pizzas disponibles (`?todas=true` para admin) |
| `POST` | `/api/pizzas` | Agregar pizza al catálogo (admin) |
| `PUT` | `/api/pizzas/{id}` | Editar pizza: precio, stock, disponibilidad (admin) |
| `DELETE` | `/api/pizzas/{id}` | Eliminar pizza sin pedidos (admin) |
| `POST` | `/api/pedidos` | Crear un nuevo pedido |
| `GET` | `/api/pedidos/{id}` | Consultar detalle de un pedido |
| `GET` | `/api/pedidos` | Listar pedidos (filtro opcional `?estado=`) |
| `PUT` | `/api/pedidos/{id}/estado` | Avanzar el estado del pedido (admin) |

---

## 6. Manejo de Errores

| Escenario | Comportamiento |
|-----------|---------------|
| Email de cliente inexistente al crear pedido | HTTP 400 con detalle |
| Pizza inexistente en el catálogo | HTTP 400 con detalle |
| Pizza no disponible (`disponible = 0`) | HTTP 400 con detalle |
| Stock insuficiente | HTTP 400; transacción revertida (rollback) |
| Email duplicado al registrar | Middleware detecta MySQL 1062 → HTTP 409 |
| Nombre de pizza duplicado | HTTP 409 |
| Pedido inexistente | HTTP 404 |
| Transición de estado fuera de orden | HTTP 409 con `estadoActual` y `estadoSolicitado` |
| Error inesperado | HTTP 500 (el middleware registra el detalle en el log) |

Toda la traducción de excepciones a respuestas HTTP está centralizada en el `ExceptionHandlingMiddleware`.

---

## 7. Flujo de Datos en la Base de Datos

```sql
-- Transacción de creación de pedido (todo o nada):
BEGIN TRANSACTION;
  UPDATE PIZZA SET stock = stock - @Cantidad
    WHERE id = @Id AND stock >= @Cantidad;         -- Paso 1: stock atómico
  INSERT INTO PEDIDO (cliente_id, estado_id, ...)  -- Paso 2
  INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, ...) -- Paso 3 (multi-insert)
  INSERT INTO HISTORIAL_ESTADO_PEDIDO (...)          -- Paso 4
COMMIT;  -- o ROLLBACK si algo falla

-- Cambio de estado:
BEGIN TRANSACTION;
  UPDATE PEDIDO SET estado_id = @nuevoEstado ...     -- Actualizar
  INSERT INTO HISTORIAL_ESTADO_PEDIDO (...)          -- Registrar
COMMIT;
```

> **Nota:** el descuento de stock ocurre **dentro** de la transacción del pedido, garantizando que nunca se descuente inventario de un pedido que no se persiste.
