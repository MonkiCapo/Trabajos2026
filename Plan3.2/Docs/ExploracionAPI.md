# Exploración Práctica de APIs REST

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Endpoints del Sistema

La tabla siguiente documenta los endpoints REST expuestos por el backend de PizzeriaAPI:

| Método | Ruta | Descripción | Códigos de respuesta |
|--------|------|-------------|---------------------|
| `POST` | `/api/clientes` | Registrar un nuevo cliente | `201 Created`, `400 Bad Request` |
| `GET` | `/api/clientes/{id}` | Obtener datos de un cliente | `200 OK`, `404 Not Found` |
| `POST` | `/api/pedidos` | Crear un nuevo pedido | `201 Created`, `400 Bad Request`, `503 Service Unavailable` |
| `GET` | `/api/pedidos/{id}` | Consultar estado de un pedido | `200 OK`, `404 Not Found` |
| `GET` | `/api/pizzas` | Listar catálogo de pizzas disponibles | `200 OK` |

---

## 2. Ejemplos de Consumo (formato cURL)

### 2.1 Registrar un Cliente

```bash
curl -X POST http://localhost:5000/api/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Juan Pérez",
    "telefono": "11-1234-5678",
    "direccion": "Av. Siempreviva 742"
  }'
```

**Response (201):**
```json
{
  "id": 1,
  "nombre": "Juan Pérez",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

### 2.2 Realizar un Pedido

```bash
curl -X POST http://localhost:5000/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": 1,
    "items": [
      { "pizzaId": 3, "cantidad": 2 },
      { "pizzaId": 5, "cantidad": 1 }
    ]
  }'
```

**Response (201) — Flujo normal:**
```json
{
  "pedidoId": 42,
  "estado": "EnPreparacion",
  "total": 3200.00,
  "fechaCreacion": "2026-07-08T20:30:00Z"
}
```

**Response (503) — Cocina no disponible:**
```json
{
  "error": "Servicio de cocina no disponible en este momento",
  "pedidoId": null,
  "codigo": "COCINA_NO_DISPONIBLE"
}
```

### 2.3 Consultar Estado del Pedido

```bash
curl http://localhost:5000/api/pedidos/42
```

**Response (200):**
```json
{
  "pedidoId": 42,
  "estado": "EnViaje",
  "cliente": {
    "id": 1,
    "nombre": "Juan Pérez"
  },
  "items": [
    { "pizza": "Muzzarella", "cantidad": 2, "precioUnitario": 1200.00 },
    { "pizza": "Napolitana", "cantidad": 1, "precioUnitario": 800.00 }
  ],
  "total": 3200.00,
  "fechaCreacion": "2026-07-08T20:30:00Z",
  "ultimaActualizacion": "2026-07-08T20:35:00Z"
}
```

### 2.4 Datos Inválidos (400 Bad Request)

```bash
curl -X POST http://localhost:5000/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{ "clienteId": null }'
```

**Response (400):**
```json
{
  "error": "Datos inválidos",
  "detalles": {
    "clienteId": ["El campo clienteId es obligatorio"],
    "items": ["Debe contener al menos un item"]
  }
}
```

---

## 3. Uso con Postman

Para probar los endpoints en Postman:

1. **Crear Colección:** `PizzeriaAPI`
2. **Variables de entorno:** `{{base_url}} = http://localhost:5000`
3. **Ejemplos de requests:**

   | Request | Método | URL | Body (raw JSON) |
   |---------|--------|-----|-----------------|
   | Crear Cliente | POST | `{{base_url}}/api/clientes` | `{ "nombre": "Juan", ... }` |
   | Crear Pedido | POST | `{{base_url}}/api/pedidos` | `{ "clienteId": 1, "items": [...] }` |
   | Consultar Pedido | GET | `{{base_url}}/api/pedidos/42` | — |
   | Listar Pizzas | GET | `{{base_url}}/api/pizzas` | — |

4. **Tests sugeridos:** Validar que el código de estado sea 201 o 200 según corresponda, y que el body contenga los campos esperados.

---

## 4. Manejo de Errores — Convenciones REST

| Código | Significado | Cuándo ocurre |
|--------|-------------|---------------|
| `200 OK` | La operación se completó exitosamente. | GET /pedidos/{id} |
| `201 Created` | El recurso se creó correctamente. | POST /pedidos, POST /clientes |
| `400 Bad Request` | El payload enviado no es válido. | Validación de esquema falla. |
| `404 Not Found` | El recurso solicitado no existe. | GET /pedidos/9999 |
| `503 Service Unavailable` | Un servicio interno no está disponible. | Timeout con Cocina. |
| `500 Internal Server Error` | Error inesperado del servidor. | Excepción no controlada. |

---

## 5. Conclusión

La API sigue las convenciones REST estándar:
- **Sustantivos en plural** para recursos (`/clientes`, `/pedidos`, `/pizzas`).
- **Códigos HTTP semánticos** para indicar el resultado.
- **JSON** como formato de intercambio.
- **Errores con estructura predecible** (campo `error` + `detalles` opcional).
