# Exploración de la API — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1
**Base URL:** `http://localhost:5183`

---

## 1. Recursos y Endpoints

| Recurso | Operaciones |
|---------|-------------|
| **Clientes** | `POST /api/clientes`, `GET /api/clientes/{id}`, `GET /api/clientes/email/{email}` |
| **Pizzas (Catálogo)** | `GET /api/pizzas`, `POST /api/pizzas`, `PUT /api/pizzas/{id}`, `DELETE /api/pizzas/{id}` |
| **Pedidos** | `POST /api/pedidos`, `GET /api/pedidos/{id}`, `GET /api/pedidos`, `PUT /api/pedidos/{id}/estado` |

---

## 2. Clientes

### 2.1 Registrar cliente

```bash
curl -X POST http://localhost:5183/api/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Juan Perez",
    "email": "juan.perez@email.com",
    "telefono": "11-1234-5678",
    "direccion": "Av. Siempreviva 742"
  }'
```

**201 Created:**
```json
{
  "id": 1,
  "nombre": "Juan Perez",
  "email": "juan.perez@email.com",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

**409 Conflict** (email duplicado):
```json
{
  "error": "Ya existe un registro con esos datos (por ejemplo, un cliente con ese email)."
}
```

### 2.2 Obtener cliente por ID

```bash
curl http://localhost:5183/api/clientes/1
```

**200 OK:** mismo shape que el registro. **404 Not Found** si no existe.

### 2.3 Obtener cliente por email

```bash
curl http://localhost:5183/api/clientes/email/juan.perez@email.com
```

**200 OK:** mismo shape. **404 Not Found** si no existe.

---

## 3. Catálogo de Pizzas

### 3.1 Listar pizzas disponibles

```bash
curl http://localhost:5183/api/pizzas
```

**200 OK:**
```json
[
  {
    "id": 1,
    "nombre": "Pizza Pepperoni",
    "tamanio": "Grande",
    "precio": 1500.00,
    "descripcion": "Muzzarella con pepperoni",
    "stock": 48,
    "disponible": true
  },
  {
    "id": 3,
    "nombre": "Pizza Muzzarella",
    "tamanio": "Grande",
    "precio": 1200.00,
    "descripcion": "Clásica de muzzarella",
    "stock": 50,
    "disponible": true
  }
]
```

> Por defecto solo devuelve pizzas con `disponible = 1`. El administrador puede ver todas con `GET /api/pizzas?todas=true`.

### 3.2 Agregar pizza (Administrador)

```bash
curl -X POST http://localhost:5183/api/pizzas \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Pizza Fugazzetta",
    "tamanio": "Grande",
    "precio": 1600.00,
    "descripcion": "Muzzarella con cebolla",
    "stock": 30,
    "disponible": true
  }'
```

**201 Created:** la pizza creada con su `id`.
**409 Conflict** si el nombre ya existe. **400 Bad Request** si falta nombre/tamaño o el precio es ≤ 0.

### 3.3 Editar pizza (precio, stock, disponibilidad)

```bash
curl -X PUT http://localhost:5183/api/pizzas/5 \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Pizza Fugazzetta",
    "tamanio": "Grande",
    "precio": 1700.00,
    "descripcion": "Muzzarella con cebolla",
    "stock": 25,
    "disponible": true
  }'
```

**200 OK:** la pizza actualizada. **404 Not Found** si no existe. **409 Conflict** si el nuevo nombre duplica a otra pizza.

### 3.4 Eliminar pizza

```bash
curl -X DELETE http://localhost:5183/api/pizzas/5
```

**200 OK:** `{ "mensaje": "Pizza eliminada correctamente." }`
**409 Conflict** si la pizza tiene ítems de pedido asociados (sugiere marcar `disponible = false`).

---

## 4. Pedidos

### 4.1 Crear pedido

```bash
curl -X POST http://localhost:5183/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{
    "clienteEmail": "juan.perez@email.com",
    "items": [
      { "pizzaNombre": "Pizza Muzzarella", "cantidad": 2 },
      { "pizzaNombre": "Pizza Pepperoni", "cantidad": 1 }
    ]
  }'
```

**201 Created:**
```json
{
  "pedidoId": 9,
  "estado": "EsperaConfirmacion",
  "total": 3900.00,
  "fechaCreacion": "2026-11-18T02:00:31"
}
```

**400 Bad Request** con detalle según la causa:

| Causa | Respuesta |
|-------|-----------|
| Items vacíos | `{ "errors": { "Items": ["Debe contener al menos un item."] } }` |
| Cliente inexistente | `{ "error": "Datos invalidos", "detalles": "No existe un cliente con el email ..." }` |
| Pizza inexistente | `{ "error": "Datos invalidos", "detalles": "La pizza \"...\" no existe en el catalogo." }` |
| Pizza no disponible | `{ "error": "Datos invalidos", "detalles": "La pizza \"...\" no esta disponible." }` |
| Stock insuficiente | `{ "error": "Datos invalidos", "detalles": "Stock insuficiente para la pizza \"...\"." }` |

> Con stock insuficiente la transacción se revierte: no queda ningún registro parcial.

### 4.2 Consultar pedido

```bash
curl http://localhost:5183/api/pedidos/9
```

**200 OK:**
```json
{
  "pedidoId": 9,
  "estado": "EnViaje",
  "cliente": {
    "id": 1,
    "nombre": "Juan Perez",
    "email": "juan.perez@email.com"
  },
  "items": [
    {
      "pizza": "Pizza Muzzarella",
      "cantidad": 2,
      "precioUnitario": 1200.00
    },
    {
      "pizza": "Pizza Pepperoni",
      "cantidad": 1,
      "precioUnitario": 1500.00
    }
  ],
  "total": 3900.00,
  "fechaCreacion": "2026-11-18T02:00:31",
  "ultimaActualizacion": "2026-11-18T02:05:10"
}
```

**404 Not Found** si el pedido no existe.

### 4.3 Listar pedidos

```bash
curl http://localhost:5183/api/pedidos
curl "http://localhost:5183/api/pedidos?estado=Entregado"
```

**200 OK:**
```json
[
  {
    "pedidoId": 9,
    "clienteId": 1,
    "estado": "Entregado",
    "total": 3900.00,
    "fechaCreacion": "2026-11-18T02:00:31",
    "ultimaActualizacion": "2026-11-18T02:05:10"
  }
]
```

**400 Bad Request** si `estado` no es un valor válido de la enumeración.

### 4.4 Cambiar estado (Administrador)

```bash
curl -X PUT http://localhost:5183/api/pedidos/9/estado \
  -H "Content-Type: application/json" \
  -d '{ "estado": "EnPreparacion" }'
```

**200 OK:**
```json
{ "pedidoId": 9, "estado": "EnPreparacion" }
```

**409 Conflict** (transición inválida):
```json
{
  "error": "Transición inválida: no se puede pasar de 'EsperaConfirmacion' a 'Entregado'. Transiciones válidas: EnPreparacion",
  "estadoActual": "EsperaConfirmacion",
  "estadoSolicitado": "Entregado"
}
```

**404 Not Found** si el pedido no existe. **400 Bad Request** si el estado no existe en la enumeración.

> **Idempotencia:** repetir el estado actual responde `200 OK` sin cambios ni registros de historial duplicados.

---

## 5. Convención de Errores

Todos los errores se generan de forma centralizada en el `ExceptionHandlingMiddleware`:

| Código | Significado | Origen típico |
|--------|-------------|----------------|
| **400** | Datos inválidos o regla de negocio rota | FluentValidation, `ArgumentException` (stock, catálogo, cliente) |
| **404** | Recurso inexistente | `PedidoNoEncontradoException`, búsquedas sin resultado |
| **409** | Conflicto de estado | Email duplicado (MySQL 1062), nombre duplicado, transición inválida, eliminación con ítems |
| **500** | Error inesperado | Excepciones no controladas (se registran en el log) |

---

## 6. Exploración de la Base de Datos

La API inicializa la base `5to_Pizzeria` automáticamente (script + migraciones de columnas idempotentes). Tablas principales:

| Tabla | Contenido |
|-------|-----------|
| `CLIENTE` | Datos de contacto del cliente |
| `PIZZA` | Catálogo con `stock` y `disponible` |
| `INGREDIENTE` / `PIZZA_INGREDIENTE` | Composición de cada pizza |
| `ESTADO_PEDIDO` | Los 4 estados del ciclo de vida |
| `PEDIDO` | Pedidos con estado actual y total |
| `ITEM_PEDIDO` | Líneas del pedido con `precio_unitario` congelado |
| `HISTORIAL_ESTADO_PEDIDO` | Auditoría de cada cambio de estado |

---

## 7. Resumen Rápido para Testing

```
 1. POST   /api/clientes                  → 201 (guarda el id)
 2. GET    /api/pizzas                    → catálogo disponible
 3. POST   /api/pedidos                   → 201 (pedidoId, EsperaConfirmacion)
 4. GET    /api/pedidos/{id}              → detalle con ítems
 5. PUT    /api/pedidos/{id}/estado       → avanzar: EnPreparacion → EnViaje → Entregado
 6. GET    /api/pedidos?estado=Entregado  → listado filtrado
 7. POST   /api/pizzas                    → nueva pizza (admin)
 8. PUT    /api/pizzas/{id}               → precio / stock / disponible (admin)
 9. DELETE /api/pizzas/{id}               → eliminar sin pedidos (admin)
10. GET    /api/clientes/email/{email}    → búsqueda por email
```
