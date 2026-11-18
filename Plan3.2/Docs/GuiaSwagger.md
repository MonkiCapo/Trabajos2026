# Guia de Testing con Swagger - PizzeriaAPI

**Proyecto:** PizzeriaAPI (Plan 3.2 - Programacion Distribuida)
**Puerto:** 5183
**Base de datos:** MySQL (5to_Pizzeria)

---

## 1. Requisitos Previos

Antes de probar con Swagger, necesitas tener corriendo los procesos del sistema:

| Proceso | Terminal | Comando | Que hace |
|---------|----------|---------|----------|
| **API (Swagger)** | Terminal 1 | `dotnet run --project src/Api.Pizzeria` | Levanta la API + Swagger en `http://localhost:5183/swagger` |
| **Cliente (opcional)** | Terminal 2 | `dotnet run --project src/Consola.Cliente` | Simula un cliente: registra, crea pedido y hace polling de estado |

> **MySQL:** Asegurate de que MySQL este corriendo en `localhost:3306` con usuario `5to_agbd` y contrasena `Trigg3rs!`, y que la base `5to_Pizzeria` exista (la API la crea e inicializa automaticamente).

> **Nota:** El ciclo de vida del pedido (cocina y reparto) se simula desde Swagger con `PUT /api/pedidos/{id}/estado`, actuando como administrador.

---

## 2. Levantar los Procesos

Necesitas **1 terminal** (2 si queres ver la consola del cliente):

```bash
# Terminal 1 - API + Swagger
dotnet run --project Api.Pizzeria

# Terminal 2 (opcional) - Cliente
dotnet run --project Consola.Cliente
```

Busca esta linea en la consola de la API:
```
Now listening on: http://localhost:5183
```

Abri en el navegador: **http://localhost:5183/swagger**

---

## 3. Flujo Completo de Testing Paso a Paso

### Paso 1: Registrar un Cliente

1. En Swagger, expandi **POST /api/clientes**
2. Hace clic en **"Try it out"**
3. En el body, pone este JSON:

```json
{
  "nombre": "Juan Perez",
  "email": "juan.perez@email.com",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

4. Hace clic en **"Execute"**
5. Verifica que la respuesta sea **201 Created**
6. **Guarda el `id`** del cliente creado

> **Nota:** El email es unico. Si intentas registrar otro cliente con el mismo email, obtendras un **409 Conflict**.

### Paso 2: Ver el Catalogo de Pizzas

1. Expandi **GET /api/pizzas**
2. Hace clic en **"Try it out"** y luego **"Execute"**
3. Veras las pizzas disponibles con sus nombres, precios y stock:

```
Pizza Pepperoni      - $1500  (stock 48)
Pizza Jamón y Queso  - $1400  (stock 50)
Pizza Muzzarella     - $1200  (stock 50)
Pizza Napolitana     - $1300  (stock 50)
```

> **Nota:** Los nombres exactos son los que debes usar en el pedido. Copialos tal cual.

### Paso 3: Crear un Pedido (por nombre de pizza)

1. Expandi **POST /api/pedidos**
2. Hace clic en **"Try it out"**
3. En el body, pone este JSON:

```json
{
  "clienteEmail": "juan.perez@email.com",
  "items": [
    { "pizzaNombre": "Pizza Muzzarella", "cantidad": 2 },
    { "pizzaNombre": "Pizza Pepperoni", "cantidad": 1 }
  ]
}
```

4. Hace clic en **"Execute"**
5. Respuestas posibles:

   - **201 Created** = Pedido creado en `EsperaConfirmacion` y stock descontado. Guarda el `pedidoId`.
   - **400 Bad Request** = Datos invalidos: email inexistente, pizza inexistente, no disponible o sin stock suficiente.

### Paso 4: Avanzar el Estado del Pedido (Administrador)

1. Expandi **PUT /api/pedidos/{id}/estado**
2. En el campo `id`, pone el `pedidoId` del Paso 3
3. En el body, pone:

```json
{ "estado": "EnPreparacion" }
```

4. Hace clic en **"Execute"** → **200 OK** con el nuevo estado
5. Repeti con `EnViaje` y luego con `Entregado`
6. Si intentas saltear estados (ej: de `EsperaConfirmacion` directo a `Entregado`), obtenes **409 Conflict** con el detalle de la transicion invalida

### Paso 5: Consultar el Estado del Pedido

1. Expandi **GET /api/pedidos/{id}**
2. En el campo `id`, pone el `pedidoId`
3. Hace clic en **"Execute"**
4. Veras el estado actual, los items (con `precioUnitario`), el total y `ultimaActualizacion`

### Paso 6: Gestionar el Catalogo (Administrador)

1. **Agregar pizza:** `POST /api/pizzas` con nombre, tamanio, precio, stock y disponible → **201 Created**
2. **Editar pizza:** `PUT /api/pizzas/{id}` para cambiar precio, stock o `disponible` → **200 OK**
3. **Retirar del menu:** `PUT` con `"disponible": false` (recomendado) o `DELETE /api/pizzas/{id}` si no tiene pedidos asociados
4. **Ver todo el catalogo:** `GET /api/pizzas?todas=true`

### Paso 7: Listar Pedidos

1. Expandi **GET /api/pedidos** → lista todos los pedidos
2. Proba el filtro: `GET /api/pedidos?estado=Entregado`

### Paso 8: Seguir el Flujo Completo (Opcional - con Consola.Cliente)

Si queres ver el flujo automatico (registro + pedido + polling), podes usar la consola del cliente:

```bash
dotnet run --project src/Consola.Cliente
```

Esto va a:
1. Registrar un cliente automaticamente (con email)
2. Crear un pedido (2 Muzzarella + 1 Pepperoni)
3. Hacer polling cada 2 segundos mostrando los cambios de estado

> **Importante:** para que la consola vea el pedido avanzar, anda cambiando el estado desde Swagger (`PUT /api/pedidos/{id}/estado`) mientras el cliente hace polling.

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

### Error: Email con formato invalido

```json
{
  "nombre": "Juan",
  "email": "no-es-un-email",
  "telefono": "11-1234-5678",
  "direccion": "Av. Siempreviva 742"
}
```

**Respuesta 400:**
```json
{
  "errors": {
    "Email": ["El formato del email no es valido."]
  }
}
```

### Error: Email duplicado

Si ya existe un cliente con ese email:

**Respuesta 409:**
```json
{
  "error": "Ya existe un registro con esos datos (por ejemplo, un cliente con ese email)."
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

### Error: Stock insuficiente

```json
{
  "clienteEmail": "juan.perez@email.com",
  "items": [
    { "pizzaNombre": "Pizza Muzzarella", "cantidad": 999 }
  ]
}
```

**Respuesta 400:**
```json
{
  "error": "Datos invalidos",
  "detalles": "Stock insuficiente para la pizza \"Pizza Muzzarella\"."
}
```

### Error: Transicion de estado invalida

```json
{ "estado": "Entregado" }
```

sobre un pedido en `EsperaConfirmacion`:

**Respuesta 409:**
```json
{
  "error": "Transición inválida: no se puede pasar de 'EsperaConfirmacion' a 'Entregado'. Transiciones válidas: EnPreparacion",
  "estadoActual": "EsperaConfirmacion",
  "estadoSolicitado": "Entregado"
}
```

---

## 5. Flujo de Comunicacion del Sistema

```
┌──────────────┐     HTTP      ┌──────────────┐      SQL       ┌──────────────┐
│  Swagger UI  │ ───────────>  │   Api.Pizza  │ ────────────>  │    MySQL     │
│  (Browser)   │ <───────────  │   (REST)     │ <────────────  │ (5to_Pizza)  │
└──────────────┘               │              │                └──────────────┘
                               │              │
┌──────────────┐     HTTP      │              │
│ Consola.     │ ───────────>  │              │
│ Cliente      │ <───────────  │              │
└──────────────┘               └──────────────┘
```

- El **administrador** opera con Swagger (catalogo + cambios de estado).
- El **cliente** opera con la Consola.Cliente (registro + pedido + polling).
- Ambos usan los mismos endpoints HTTP de la API.

---

## 6. Troubleshooting

| Problema | Solucion |
|----------|----------|
| Swagger no carga | Verifica que la API este corriendo en puerto 5183 |
| Error de conexion MySQL | Verifica que MySQL este corriendo en localhost:3306 con las credenciales del proyecto |
| Base de datos no existe | La API la crea automaticamente; si falla, ejecuta el script.sql en HeidiSQL o MySQL Workbench |
| Pedido queda en `EsperaConfirmacion` | Es el estado inicial por diseño; avanzalo con `PUT /api/pedidos/{id}/estado` |
| Error 400 con `errors` | FluentValidation rechazo los datos |
| Error 400 con `detalles` | Regla de negocio rota (pizza inexistente, stock, cliente) |
| Error 409 | Conflicto: email o nombre duplicado, transicion invalida, eliminacion con pedidos |
| Error 404 | El recurso (pedido, cliente o pizza) no existe |

---

## 7. Resumen de Endpoints para Testing Rapido

```
 1. POST   /api/clientes                  -> Registrar cliente
 2. GET    /api/clientes/{id}             -> Verificar datos del cliente por ID
 3. GET    /api/clientes/email/{email}    -> Verificar datos del cliente por email
 4. GET    /api/pizzas                    -> Ver catalogo (solo disponibles)
 5. GET    /api/pizzas?todas=true         -> Ver catalogo completo (admin)
 6. POST   /api/pizzas                    -> Agregar pizza (admin)
 7. PUT    /api/pizzas/{id}               -> Editar precio/stock/disponible (admin)
 8. DELETE /api/pizzas/{id}               -> Eliminar pizza sin pedidos (admin)
 9. POST   /api/pedidos                   -> Crear pedido (clienteEmail + items)
10. GET    /api/pedidos/{id}              -> Consultar estado del pedido
11. GET    /api/pedidos?estado=...        -> Listar pedidos (filtro opcional)
12. PUT    /api/pedidos/{id}/estado       -> Avanzar estado (admin)
```
