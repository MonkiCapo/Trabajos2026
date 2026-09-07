# Flujo Funcional del Sistema — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Descripción General

El sistema es una aplicación de gestión de pedidos de pizzería compuesta por un backend REST central (la API) y consumidores HTTP (aplicación cliente, Scalar o cualquier cliente HTTP). Toda la comunicación se realiza mediante HTTP/JSON, sin servicios externos.

---

## 2. Arquitectura de Procesos

```
┌─────────────────────┐
│   Cliente (HTTP)    │  ← Aplicación C# / Scalar / curl
└─────────┬───────────┘
          │ HTTP (JSON)
          ▼
┌─────────────────────┐
│   Api.Pizzeria      │
│   (Backend Central) │
│                     │
│   - Minimal API     │
│   - PedidoService   │
│   - Repositorios    │
└─────────┬───────────┘
          │ SQL
          ▼
┌─────────────────────┐
│   MySQL (5to_Pizza) │
└─────────────────────┘
```

---

## 3. Flujo Completo de un Pedido

### Fase 1: Registro del Cliente

```
1. El cliente registra sus datos con POST /api/clientes
2. El backend valida con FluentValidation y persiste en MySQL
3. El cliente queda identificado por su email
```

### Fase 2: Creación del Pedido

```
1. CLIENTE envía POST /api/pedidos
   └─ Body: { clienteEmail, items[] }

2. BACKEND valida:
   ├─ FluentValidation (PedidoRequestValidator)
   ├─ Verifica que el cliente existe en MySQL
   └─ Verifica que cada pizza del catálogo existe

3. BACKEND crea el pedido en MySQL (transacción):
   ├─ PEDIDO (estado: EsperaConfirmacion)
   ├─ ITEM_PEDIDO (cada pizza con precio unitario)
   └─ HISTORIAL_ESTADO_PEDIDO (registro del cambio)

4. BACKEND responde HTTP 201 con { pedidoId, estado, total }
```

### Fase 3: Transiciones de Estado (por HTTP)

```
1. ADMIN envía PATCH /api/pedidos/{id}/estado
   └─ Body: { estado: "EnPreparacion", observacion: "..." }

2. BACKEND verifica que el pedido existe

3. BACKEND actualiza el estado y el historial (transacción)

4. BACKEND responde HTTP 200 con { pedidoId, estado }

   Repetir para: EnPreparacion → EnViaje → Entregado
   (o Cancelado si se anula)
```

### Fase 4: Consulta y Seguimiento

```
- El cliente puede consultar el estado vía GET /api/pedidos/{id}
- Una aplicación cliente puede implementar polling cada N segundos
- Estados posibles:
  • EsperaConfirmacion
  • EnPreparacion
  • EnViaje
  • Entregado
  • Cancelado
```

---

## 4. Máquina de Estados del Pedido

```
                    ┌──────────────────┐
                    │ EsperaConfirmacion│
                    └────────┬─────────┘
                             │ PATCH { estado: "EnPreparacion" }
                             ▼
                    ┌──────────────────┐
                    │  EnPreparacion   │
                    └────────┬─────────┘
                             │ PATCH { estado: "EnViaje" }
                             ▼
                    ┌──────────────────┐
                    │    EnViaje       │
                    └────────┬─────────┘
                             │ PATCH { estado: "Entregado" }
                             ▼
                    ┌──────────────────┐
                    │   Entregado      │
                    └──────────────────┘

    Transición de cancelación (desde cualquier estado):
    Cualquier estado ──PATCH { estado: "Cancelado" }──► Cancelado
```

---

## 5. Endpoints de la API REST

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/clientes` | Registrar nuevo cliente |
| `GET` | `/api/clientes/{id}` | Obtener cliente por ID |
| `GET` | `/api/clientes/email/{email}` | Obtener cliente por email |
| `GET` | `/api/pizzas` | Catálogo de pizzas disponibles |
| `POST` | `/api/pedidos` | Crear un nuevo pedido |
| `GET` | `/api/pedidos/{id}` | Consultar estado de un pedido |
| `PATCH` | `/api/pedidos/{id}/estado` | Transicionar el estado de un pedido |

---

## 6. Manejo de Errores

| Escenario | Comportamiento |
|-----------|---------------|
| Email de cliente inexistente | HTTP 400 con mensaje descriptivo |
| Pizza inexistente en catálogo | Excepción `ArgumentException` → HTTP 400 |
| Email duplicado al registrar | MySQL error 1062 → HTTP 400 |
| Pedido inexistente en GET/PATCH | HTTP 404 |
| Error inesperado del servidor | HTTP 500 con log |

---

## 7. Flujo de Datos en la Base de Datos

```sql
-- Transacción de creación de pedido:
BEGIN TRANSACTION;
  INSERT INTO PEDIDO (cliente_id, estado_id, ...)     -- Paso 1
  INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, ...)  -- Paso 2
  INSERT INTO HISTORIAL_ESTADO_PEDIDO (...)            -- Paso 3
COMMIT;

-- Transición de estado (PATCH):
BEGIN TRANSACTION;
  UPDATE PEDIDO SET estado_id = @nuevoEstado ...       -- Actualizar
  INSERT INTO HISTORIAL_ESTADO_PEDIDO (...)            -- Registrar
COMMIT;
```