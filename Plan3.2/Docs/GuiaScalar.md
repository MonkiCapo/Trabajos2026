# Guia de Testing con Scalar - PizzeriaAPI

**Proyecto:** PizzeriaAPI (Plan 3.2)
**Puerto:** 5183
**Base de datos:** MySQL (5to_Pizzeria)

---

## 1. Requisitos Previos

Antes de probar con Scalar, solo necesitas la API corriendo:

| Proceso | Terminal | Comando | Que hace |
|---------|----------|---------|----------|
| **API (Scalar)** | Terminal 1 | `dotnet run --project src/Api.Pizzeria` | Levanta la API + Scalar en `http://localhost:5183/scalar` |

> **MySQL:** Asegurate de que MySQL este corriendo en `localhost:3306` con usuario `5to_agbd` y contrasena `Trigg3rs!`, y que la base `5to_Pizzeria` exista.

---

## 2. Levantar la API

Abre una terminal desde la carpeta `src/`:

```bash
dotnet run --project Api.Pizzeria
```

Busca esta linea en la consola de la API:
```
Now listening on: http://localhost:5183
```

Abri en el navegador: **http://localhost:5183/scalar**

---

## 3. Flujo Completo de Testing Paso a Paso

### Paso 1: Registrar un Cliente

1. En Scalar, expandi **POST /api/clientes** (hace clic sobre el endpoint para abrir el panel de request).
2. En el panel derecho (Testing), pone este JSON en el body:

```json
{
  "nombre": "Juan Perez",
  "email": "juan.perez@email.com",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

3. Hace clic en **"Send"** (botón de envío del request en la parte superior del panel).
4. Verifica que la respuesta sea **201 Created**

> **Nota:** El email es unico. Si intentas registrar otro cliente con el mismo email, obtendras un 400.

### Paso 2: Ver el Catalogo de Pizzas

1. Expandi **GET /api/pizzas** y hace clic en **"Send"** (no necesita body).
2. Veras las pizzas disponibles con sus nombres:

```
Pizza Pepperoni      - $1500
Pizza Jamón y Queso  - $1400
Pizza Muzzarella     - $1200
Pizza Napolitana     - $1300
```

> **Nota:** Los nombres exactos son los que debes usar en el pedido. Copialos tal cual.

### Paso 3: Crear un Pedido

1. Expandi **POST /api/pedidos** (hace clic sobre el endpoint).
2. En el body, pone este JSON:

```json
{
  "clienteEmail": "juan.perez@email.com",
  "items": [
    { "pizzaNombre": "Pizza Muzzarella", "cantidad": 2 },
    { "pizzaNombre": "Pizza Pepperoni", "cantidad": 1 }
  ]
}
```

3. Hace clic en **"Send"**.
4. Respuesta esperada:

   - **201 Created** = Pedido creado en estado `EsperaConfirmacion`. **Guarda el `pedidoId`**.
   - **400 Bad Request** = Datos invalidos (email inexistente o pizza no encontrada).

### Paso 4: Transicionar el Estado del Pedido (PATCH)

1. Expandi **PATCH /api/pedidos/{id}/estado**.
2. En el campo `id` (parámetro de ruta), pone el `pedidoId` del Paso 3.
3. En el body, repiti cada estado en orden (simula el avance del pedido):

```json
{ "estado": "EnPreparacion", "observacion": "Cocina aceptó el pedido" }
```

```json
{ "estado": "EnViaje", "observacion": "Pizzas listas, en camino" }
```

```json
{ "estado": "Entregado", "observacion": "Reparto entregó al cliente" }
```

4. Cada llamada (clic en **"Send"**) deberia responder **200 OK** con el nuevo estado.

> Tambien podes cancelar el pedido con `{ "estado": "Cancelado", "observacion": "..." }`.

### Paso 5: Consultar el Estado del Pedido

1. Expandi **GET /api/pedidos/{id}**.
2. En el campo `id`, pone el `pedidoId`.
3. Hace clic en **"Send"**.
4. Veras el estado actual del pedido (deberia ser `Entregado` si completaste el Paso 4), los items y los datos del cliente.

### Paso 6: Buscar Cliente por Email

1. Expandi **GET /api/clientes/email/{email}** y pone el email `juan.perez@email.com`.
2. Hace clic en **"Send"**.
3. Veras los datos completos del cliente.

---

## 4. Probando Validaciones (FluentValidation)

### Error: Cliente sin email

```json
{
  "nombre": "Juan",
  "email": "",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

**Respuesta 400:**
```json
{
  "errors": {
    "Email": ["El email es obligatorio."]
  }
}
```

### Error: Pedido sin items

```json
{
  "clienteEmail": "juan.perez@email.com",
  "items": []
}
```

**Respuesta 400:**
```json
{
  "errors": {
    "Items": ["Debe contener al menos un item."]
  }
}
```

### Error: Pizza que no existe

```json
{
  "clienteEmail": "juan.perez@email.com",
  "items": [
    { "pizzaNombre": "Fugazzetta", "cantidad": 1 }
  ]
}
```

**Respuesta 400:**
```json
{
  "error": "Datos invalidos",
  "detalles": "La pizza \"Fugazzetta\" no existe en el catalogo."
}
```

---

## 5. Flujo de Comunicacion

```
┌──────────────┐     HTTP      ┌──────────────┐     SQL      ┌──────────────┐
│   Scalar     │ ───────────>  │   Api.Pizza  │ ───────────>  │   MySQL      │
│  (Browser)   │ <───────────  │   (REST)     │ <───────────  │  (5to_Pizza) │
└──────────────┘               └──────────────┘               └──────────────┘
```

---

## 6. Troubleshooting

| Problema | Solucion |
|----------|----------|
| Scalar no carga | Verifica que la API este corriendo en puerto 5183 |
| Error de conexion MySQL | Verifica que MySQL este corriendo en localhost:3306 |
| Base de datos no existe | Ejecuta el script.sql en HeidiSQL o MySQL Workbench |
| Pedido queda en `EsperaConfirmacion` | Es el estado inicial normal. Usa el PATCH para avanzarlo |
| Error 400 con `errors` | FluentValidation rechazo los datos |
| `email` duplicado | Usa otro email, el campo es unico en la BD |
| Pedido no encontrado (404) | Verifica el `pedidoId` usado en la URL |

---

## 7. Resumen de Endpoints para Testing Rapido

```
1. GET   /api/pizzas                    -> Ver catalogo (nombres disponibles)
2. POST  /api/clientes                  -> Registrar cliente (nombre, email, telefono, direccion)
3. GET   /api/clientes/{id}             -> Verificar datos del cliente por ID
4. GET   /api/clientes/email/{email}    -> Verificar datos del cliente por email
5. POST  /api/pedidos                   -> Crear pedido (clienteEmail + items)
6. GET   /api/pedidos/{id}              -> Consultar estado del pedido
7. PATCH /api/pedidos/{id}/estado       -> Transicionar estado (EnPreparacion/EnViaje/Entregado/Cancelado)
```