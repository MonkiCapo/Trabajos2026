# Ejemplo de uso: Transiciones de estado por HTTP

Tras eliminar los TCP Sockets, el estado de los pedidos se gestiona **solo por HTTP** a través de la API. Este archivo es una guía de ejemplo para seguir el ciclo de vida de un pedido a mano.

## Resumen del ciclo de vida

```
EsperaConfirmacion --(PATCH EnPreparacion)--> EnPreparacion --(PATCH EnViaje)--> EnViaje --(PATCH Entregado)--> Entregado
        \--(PATCH Cancelado)--> Cancelado
```

El pedido **nace en `EsperaConfirmacion`** al crearse con `POST /api/pedidos`. Desde ahí, cada avance se hace con un `PATCH` al endpoint de estado.

---

## Endpoints relevantes

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/pedidos` | Crear pedido (nace en `EsperaConfirmacion`) |
| `GET` | `/api/pedidos/{id}` | Consultar estado de un pedido |
| `PATCH` | `/api/pedidos/{id}/estado` | Cambiar estado (transición manual) |

Estados válidos para el `PATCH`: `EnPreparacion`, `EnViaje`, `Entregado`, `Cancelado`.

---

## Ejemplos con curl

### 1. Crear un pedido

```bash
curl -X POST http://localhost:5183/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{
    "clienteEmail": "juan@correo.com",
    "items": [
      { "pizzaNombre": "Muzzarella", "cantidad": 2 },
      { "pizzaNombre": "Napolitana", "cantidad": 1 }
    ]
  }'
```

Respuesta esperada (el pedido queda en `EsperaConfirmacion`):
```json
{ "pedidoId": 1, "estado": "EsperaConfirmacion", "total": 1234.00, "fechaCreacion": "..." }
```

### 2. Iniciar preparación (la "cocina" arranca)

```bash
curl -X PATCH http://localhost:5183/api/pedidos/1/estado \
  -H "Content-Type: application/json" \
  -d '{ "estado": "EnPreparacion", "observacion": "Cocina aceptó el pedido" }'
```

### 3. Marcar pedido listo / en viaje

```bash
curl -X PATCH http://localhost:5183/api/pedidos/1/estado \
  -H "Content-Type: application/json" \
  -d '{ "estado": "EnViaje", "observacion": "Cocina terminó, reparto en camino" }'
```

### 4. Marcar entregado

```bash
curl -X PATCH http://localhost:5183/api/pedidos/1/estado \
  -H "Content-Type: application/json" \
  -d '{ "estado": "Entregado", "observacion": "Reparto entregó al cliente" }'
```

### 5. Consultar el pedido en cualquier momento

```bash
curl http://localhost:5183/api/pedidos/1
```

---

## Notas para tu refactor

- El endpoint `PATCH /api/pedidos/{id}/estado` ya usa `PedidoService.ActualizarEstadoAsync`, que persiste el cambio **y su historial** en la base de datos.
- `ActualizarEstadoAsync` está definido en `IPedidoService` (Core.Pizzeria) — es el único punto de entrada para cambiar estados.
- Podés consumir este mismo endpoint desde una UI futura o una consola de administración sin escribir nada de sockets.
- El DTO del body es `ActualizarEstadoRequest` (en `Core.Pizzeria/DTOs`):
  ```csharp
  public class ActualizarEstadoRequest
  {
      public EstadoPedido Estado { get; set; }
      public string Observacion { get; set; } = string.Empty;
  }
  ```
