# Exploracion Practica de APIs REST

**Proyecto:** PizzeriaAPI
**Curso:** Computacion - ET12 DE1

---

## 1. Endpoints del Sistema

| Metodo | Ruta | Descripcion | Codigos de respuesta |
|--------|------|-------------|---------------------|
| `POST` | `/api/clientes` | Registrar un nuevo cliente (con email) | `201 Created`, `400 Bad Request` |
| `GET` | `/api/clientes/{id}` | Obtener datos de un cliente por ID | `200 OK`, `404 Not Found` |
| `GET` | `/api/clientes/email/{email}` | Obtener datos de un cliente por email | `200 OK`, `404 Not Found` |
| `POST` | `/api/pedidos` | Crear un nuevo pedido (por email del cliente y nombre de pizza) | `201 Created`, `400 Bad Request`, `503 Service Unavailable` |
| `GET` | `/api/pedidos/{id}` | Consultar estado de un pedido | `200 OK`, `404 Not Found` |
| `GET` | `/api/pizzas` | Listar catalogo de pizzas disponibles | `200 OK` |

> **Swagger UI:** Disponible en `http://localhost:5183/swagger` cuando la API esta corriendo.

---

## 2. Ejemplos de Consumo (formato cURL)

### 2.1 Registrar un Cliente

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

**Response (201):**
```json
{
  "id": 1,
  "nombre": "Juan Perez",
  "email": "juan.perez@email.com",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

### 2.2 Buscar Cliente por Email

```bash
curl http://localhost:5183/api/clientes/email/juan.perez@email.com
```

**Response (200):**
```json
{
  "id": 1,
  "nombre": "Juan Perez",
  "email": "juan.perez@email.com",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

### 2.3 Consultar Catalogo de Pizzas

```bash
curl http://localhost:5183/api/pizzas
```

**Response (200):**
```json
[
  { "id": 1, "nombre": "Pizza Pepperoni", "tamanio": "Grande", "precio": 1500.00, "descripcion": "..." },
  { "id": 2, "nombre": "Pizza Jamón y Queso", "tamanio": "Grande", "precio": 1400.00, "descripcion": "..." },
  { "id": 3, "nombre": "Pizza Muzzarella", "tamanio": "Grande", "precio": 1200.00, "descripcion": "..." },
  { "id": 4, "nombre": "Pizza Napolitana", "tamanio": "Grande", "precio": 1300.00, "descripcion": "..." }
]
```

### 2.4 Realizar un Pedido (por nombre de pizza)

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

**Response (201) - Flujo normal:**
```json
{
  "pedidoId": 42,
  "estado": "EnPreparacion",
  "total": 3900.00,
  "fechaCreacion": "2026-07-13T20:30:00Z"
}
```

**Response (503) - Cocina no disponible:**
```json
{
  "error": "Servicio de cocina no disponible en este momento",
  "pedidoId": 42,
  "codigo": "COCINA_NO_DISPONIBLE"
}
```

### 2.5 Consultar Estado del Pedido

```bash
curl http://localhost:5183/api/pedidos/42
```

**Response (200):**
```json
{
  "pedidoId": 42,
  "estado": "EnViaje",
  "cliente": {
    "id": 1,
    "nombre": "Juan Perez",
    "email": "juan.perez@email.com"
  },
  "items": [
    { "pizza": "Pizza Muzzarella", "cantidad": 2, "precioUnitario": 1200.00 },
    { "pizza": "Pizza Pepperoni", "cantidad": 1, "precioUnitario": 1500.00 }
  ],
  "total": 3900.00,
  "fechaCreacion": "2026-07-13T20:30:00Z",
  "ultimaActualizacion": "2026-07-13T20:35:00Z"
}
```

### 2.6 Datos Invalidos (400 Bad Request - FluentValidation)

```bash
curl -X POST http://localhost:5183/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{ "clienteId": 0, "items": [] }'
```

**Response (400):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ClienteId": ["El campo clienteId debe ser mayor a 0."],
    "Items": ["Debe contener al menos un item."]
  }
}
```

---

## 3. Uso con Postman

1. **Crear Coleccion:** `PizzeriaAPI`
2. **Variables de entorno:** `{{base_url}} = http://localhost:5183`
3. **Ejemplos de requests:**

   | Request | Metodo | URL | Body (raw JSON) |
   |---------|--------|-----|-----------------|
   | Crear Cliente | POST | `{{base_url}}/api/clientes` | `{ "nombre": "...", "email": "...", ... }` |
   | Buscar por Email | GET | `{{base_url}}/api/clientes/email/{email}` | - |
   | Listar Pizzas | GET | `{{base_url}}/api/pizzas` | - |
    | Crear Pedido | POST | `{{base_url}}/api/pedidos` | `{ "clienteEmail": "juan@email.com", "items": [{ "pizzaNombre": "Pizza Muzzarella", "cantidad": 2 }] }` |
   | Consultar Pedido | GET | `{{base_url}}/api/pedidos/42` | - |

---

## 4. Manejo de Errores - Convenciones REST

| Codigo | Significado | Cuando ocurre |
|--------|-------------|---------------|
| `200 OK` | La operacion se completo exitosamente. | GET /pedidos/{id} |
| `201 Created` | El recurso se creo correctamente. | POST /pedidos, POST /clientes |
| `400 Bad Request` | El payload enviado no es valido (FluentValidation). | Validacion de esquema falla. |
| `404 Not Found` | El recurso solicitado no existe. | GET /pedidos/9999 |
| `503 Service Unavailable` | Un servicio interno no esta disponible. | Timeout con Cocina. |
| `500 Internal Server Error` | Error inesperado del servidor. | Excepcion no controlada. |

---

## 5. Validaciones (FluentValidation)

### ClienteRequest (POST /api/clientes)

| Campo | Regla |
|-------|-------|
| `nombre` | Obligatorio, max 100 caracteres |
| `email` | Obligatorio, max 150 caracteres, formato email valido, unico en la BD |
| `telefono` | Obligatorio, max 20 caracteres, solo numeros/espacios/guiones/+ |
| `direccion` | Obligatoria, max 200 caracteres |

### PedidoRequest (POST /api/pedidos)

| Campo | Regla |
|-------|-------|
| `clienteEmail` | Obligatorio, formato email valido |
| `items` | Obligatorio, al menos 1 item |
| `items[].pizzaNombre` | Obligatorio, max 100 caracteres |
| `items[].cantidad` | Mayor a 0, maximo 100 |

---

## 6. Base de Datos

El proyecto usa **MySQL** (via MySqlConnector). La conexion esta configurada en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=5to_Pizzeria;User=5to_agbd;Password=Trigg3rs!;"
  }
}
```

---

## 7. Conclusion

La API sigue las convenciones REST estandar:
- **Sustantivos en plural** para recursos (`/clientes`, `/pedidos`, `/pizzas`).
- **Codigos HTTP semanticos** para indicar el resultado.
- **JSON** como formato de intercambio.
- **Errores con estructura predecible** (campo `errors` con diccionario de FluentValidation).
- **Pedidos por nombre de pizza** en vez de ID para mayor legibilidad.
- **Cliente con email unico** para identificacion unica.
