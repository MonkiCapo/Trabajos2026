# Flujo Funcional del Sistema — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Descripción General

El sistema es una aplicación distribuida de gestión de pedidos de pizzería compuesta por cuatro procesos independientes que se comunican mediante HTTP REST y TCP Sockets.

---

## 2. Arquitectura de Procesos

```
┌─────────────────────┐
│   Consola.Cliente   │  ← Interfaz del usuario final
│   (App de consola)  │
└─────────┬───────────┘
          │ HTTP (JSON)
          ▼
┌─────────────────────┐       TCP Socket        ┌──────────────────┐
│   Api.Pizzeria      │◄────────────────────────►│  Consola.Cocina  │
│   (Backend Central) │       TCP Socket        │  (Simula cocción)│
│                     │◄────────────────────────►├──────────────────┤
│   - Minimal API     │                         │ Consola.Reparto  │
│   - SocketServer    │                         │ (Simula entrega) │
│   - PedidoService   │                         └──────────────────┘
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
1. El cliente inicia la Consola.Cliente
2. Selecciona opción 1 (Registrar) o 2 (Buscar)
3. La app envía POST /api/clientes o GET /api/clientes/{id}
4. El backend valida con FluentValidation y persiste en MySQL
5. El cliente queda "activo" en la consola para operar
```

### Fase 2: Creación del Pedido (Flujo Principal)

```
┌─────────────────────────────────────────────────────────────────────┐
│                    FLUJO DE CREACIÓN DE PEDIDO                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. CLIENTE envía POST /api/pedidos                                │
│     └─ Body: { clienteEmail, items[] }                             │
│                                                                     │
│  2. BACKEND valida:                                                 │
│     ├─ FluentValidation (PedidoRequestValidator)                   │
│     ├─ Verifica que el cliente existe en MySQL                     │
│     └─ Verifica que cada pizza del catálogo existe                 │
│                                                                     │
│  3. BACKEND crea el pedido en MySQL:                               │
│     ├─ PEDIDO (estado: EsperaConfirmacion)                         │
│     ├─ ITEM_PEDIDO (cada pizza con precio unitario)                │
│     └─ HISTORIAL_ESTADO_PEDIDO (registro del cambio)               │
│                                                                     │
│  4. BACKEND envía por socket TCP a COCINA:                         │
│     └─ { accion: "nuevo_pedido", pedidoId, items[] }               │
│                                                                     │
│  5. COCINA recibe y responde ACK inmediato:                        │
│     └─ { accion: "ack", pedidoId, status: "recibido" }            │
│                                                                     │
│  6. BACKEND actualiza estado a EnPreparacion                       │
│     └─ Responde HTTP 201 al cliente                                │
│                                                                     │
│  7. COCINA simula cocción (6 segundos en hilo secundario)          │
│     └─ Envía evento: { accion: "pedido_preparado", pedidoId }     │
│                                                                     │
│  8. BACKEND recibe evento y actualiza estado a EnViaje             │
│                                                                     │
│  9. BACKEND envía por socket a REPARTO:                            │
│     └─ { accion: "asignar_entrega", pedidoId, direccion }         │
│                                                                     │
│ 10. REPARTO recibe y responde ACK inmediato                        │
│                                                                     │
│ 11. REPARTO simula entrega (6 segundos en hilo secundario)         │
│     └─ Envía evento: { accion: "pedido_entregado", pedidoId }     │
│                                                                     │
│ 12. BACKEND recibe evento y actualiza estado a Entregado           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Fase 3: Consulta y Seguimiento

```
- El cliente puede consultar el estado vía GET /api/pedidos/{id}
- La Consola.Cliente implementa polling cada 2 segundos
- Muestra los cambios de estado en tiempo real con colores:
  • Amarillo = EsperaConfirmacion
  • Azul     = EnPreparacion
  • Magenta  = EnViaje
  • Verde    = Entregado
  • Rojo     = Cancelado
```

---

## 4. Máquina de Estados del Pedido

```
                    ┌──────────────────┐
                    │ EsperaConfirmacion│
                    └────────┬─────────┘
                             │ ACK de Cocina recibido
                             ▼
                    ┌──────────────────┐
                    │  EnPreparacion   │
                    └────────┬─────────┘
                             │ Cocina envía "pedido_preparado"
                             ▼
                    ┌──────────────────┐
                    │    EnViaje       │
                    └────────┬─────────┘
                             │ Reparto envía "pedido_entregado"
                             ▼
                    ┌──────────────────┐
                    │   Entregado      │
                    └──────────────────┘

    Transición de error:
    EsperaConfirmacion ──(timeout/rechazo)──► Cancelado
```

---

## 5. Protocolo de Comunicación Socket

### Handshake de conexión
```
1. Cliente TCP se conecta al puerto 7000
2. Envía: { "accion": "identificar", "tipo": "cocina" }  ó  { "accion": "identificar", "tipo": "reparto" }
3. El servidor lo registra como cliente activo
4. Queda en bucle de escucha de mensajes
```

### Protocolo ACK (Confirmación)
```
Backend envía pedido → Cliente responde ACK → Backend confirma
                     ↻ Si no hay ACK en 5 segundos → Timeout → Pedido cancelado
```

### Protocolo de Eventos
```
Cocina:    { "accion": "pedido_preparado", "pedidoId": N }
Reparto:   { "accion": "pedido_entregado", "pedidoId": N }
```

---

## 6. Endpoints de la API REST

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/clientes` | Registrar nuevo cliente |
| `GET` | `/api/clientes/{id}` | Obtener cliente por ID |
| `GET` | `/api/clientes/email/{email}` | Obtener cliente por email |
| `GET` | `/api/pizzas` | Catálogo de pizzas disponibles |
| `POST` | `/api/pedidos` | Crear un nuevo pedido |
| `GET` | `/api/pedidos/{id}` | Consultar estado de un pedido |

---

## 7. Manejo de Errores

| Escenario | Comportamiento |
|-----------|---------------|
| Cocina no conectada | Se lanza `SocketException` → Pedido cancelado → HTTP 503 |
| Cocina no responde ACK en 5s | Timeout → Pedido cancelado → HTTP 503 |
| Reparto no conectado | Solo se registra warning (el pedido sigue en EnViaje) |
| Reparto no responde ACK | Solo se registra warning (el pedido sigue en EnViaje) |
| Email de cliente inexistente | HTTP 400 con mensaje descriptivo |
| Pizza inexistente en catálogo | Excepción `ArgumentException` → HTTP 400 |
| Email duplicado al registrar | MySQL error 1062 → HTTP 400 |

---

## 8. Flujo de Datos en la Base de Datos

```sql
-- Transacción de creación de pedido:
BEGIN TRANSACTION;
  INSERT INTO PEDIDO (cliente_id, estado_id, ...)     -- Paso 1
  INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, ...)  -- Paso 2
  INSERT INTO HISTORIAL_ESTADO_PEDIDO (...)            -- Paso 3
COMMIT;

-- Transición de estado (posterior):
BEGIN TRANSACTION;
  UPDATE PEDIDO SET estado_id = @nuevoEstado ...       -- Actualizar
  INSERT INTO HISTORIAL_ESTADO_PEDIDO (...)            -- Registrar
COMMIT;
```

> **Nota:** La interacción por socket se realiza **después** de commitear la transacción, para no bloquear la base de datos durante la comunicación de red.
