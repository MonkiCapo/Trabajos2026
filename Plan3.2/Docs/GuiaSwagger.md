# Guia de Testing con Swagger - PizzeriaAPI

**Proyecto:** PizzeriaAPI (Plan 3.2 - Programacion Distribuida)
**Puerto:** 5183
**Base de datos:** MySQL (5to_Pizzeria)

---

## 1. Requisitos Previos

Antes de probar con Swagger, necesitas tener corriendo los procesos del sistema distribuido:

| Proceso | Terminal | Comando | Que hace |
|---------|----------|---------|----------|
| **API (Swagger)** | Terminal 1 | `dotnet run --project src/Api.Pizzeria` | Levanta la API + Swagger en `http://localhost:5183/swagger` |
| **Cocina** | Terminal 2 | `dotnet run --project src/Consola.Cocina` | Se conecta por TCP al puerto 7000 y responde pedidos |
| **Reparto** | Terminal 3 | `dotnet run --project src/Consola.Reparto` | Se conecta por TCP al puerto 7000 y gestiona entregas |
| **Cliente** | Terminal 4 | `dotnet run --project src/Consola.Cliente` | Simula un cliente: registra, crea pedido y hace polling de estado |

> **Importante:** Sin la Cocina corriendo, los pedidos seran **cancelados automaticamente** porque no se recibe ACK en 5 segundos.

> **MySQL:** Asegurate de que MySQL este corriendo en `localhost:3306` con usuario `root` sin contrasena, y que la base `5to_Pizzeria` exista.

---

## 2. Levantar Todos los Procesos

Necesitas **4 terminales** abiertas desde la carpeta `src/`:

```bash
# Terminal 1 - API + Swagger
dotnet run --project Api.Pizzeria

# Terminal 2 - Cocina
dotnet run --project Consola.Cocina

# Terminal 3 - Reparto
dotnet run --project Consola.Reparto

# Terminal 4 - Cliente (simula un pedido automatico)
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
6. **Guarda el `id`** del cliente creado (lo necesitas para el pedido)

> **Nota:** El email es unico. Si intentas registrar otro cliente con el mismo email, obtendras un 400.

### Paso 2: Ver el Catalogo de Pizzas

1. Expandi **GET /api/pizzas**
2. Hace clic en **"Try it out"** y luego **"Execute"**
3. Veras las pizzas disponibles con sus nombres:

```
Pizza Pepperoni      - $1500
Pizza Jamón y Queso  - $1400
Pizza Muzzarella     - $1200
Pizza Napolitana     - $1300
```

> **Nota:** Los nombres exactos son los que debes usar en el pedido. Copialos tal cual.

### Paso 3: Crear un Pedido (por nombre de pizza)

1. Expandi **POST /api/pedidos**
2. Hace clic en **"Try it out"**
3. En el body, pone este JSON (reemplaza `clienteId` con el ID del Paso 1):

```json
{
  "clienteId": 1,
  "items": [
    { "pizzaNombre": "Pizza Muzzarella", "cantidad": 2 },
    { "pizzaNombre": "Pizza Pepperoni", "cantidad": 1 }
  ]
}
```

4. Hace clic en **"Execute"**
5. Respuestas posibles:

   - **201 Created** = Pedido aceptado, cocina lo recibio. Guarda el `pedidoId`.
   - **503 Service Unavailable** = La cocina no esta corriendo o no respondio a tiempo.
   - **400 Bad Request** = Datos invalidos.

### Paso 4: Consultar el Estado del Pedido

1. Expandi **GET /api/pedidos/{id}**
2. Hace clic en **"Try it out"**
3. En el campo `id`, pone el `pedidoId` del Paso 3
4. Hace clic en **"Execute"**
5. Veras el estado actual del pedido, los items y los datos del cliente (incluyendo email)

### Paso 5: Buscar Cliente por Email

1. Expandi **GET /api/clientes/email/{email}**
2. Hace clic en **"Try it out"**
3. En el campo `email`, pone `juan.perez@email.com`
4. Hace clic en **"Execute"**
5. Veras los datos completos del cliente

### Paso 6: Seguir el Flujo Completo (Opcional - con Consola.Cliente)

Si queres ver el flujo completo automatico (registro + pedido + polling), podes usar la consola del cliente:

```bash
dotnet run --project src/Consola.Cliente
```

Esto va a:
1. Registrar un cliente automaticamente (con email)
2. Crear un pedido (2 Muzzarella + 1 Pepperoni)
3. Hacer polling cada 2 segundos mostrando los cambios de estado

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

**Respuesta 400:**
```json
{
  "error": "Ya existe un cliente con ese email."
}
```

### Error: Pedido sin items

```json
{
  "clienteId": 1,
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
  "clienteId": 1,
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

## 5. Flujo de Comunicacion Distribuida

```
┌──────────────┐     HTTP      ┌──────────────┐     TCP:7000    ┌──────────────┐
│  Swagger UI  │ ───────────>  │   Api.Pizza  │ ─────────────>  │   Cocina     │
│  (Browser)   │ <───────────  │   (REST)     │ <─────────────  │   (Console)  │
└──────────────┘               │              │                 └──────────────┘
                               │   MySQL      │     TCP:7000    └──────────────┘
                               │  (5to_Pizza) │ ─────────────>  │   Reparto    │
                               │              │ <─────────────  │   (Console)  │
                               └──────────────┘
```

---

## 6. Troubleshooting

| Problema | Solucion |
|----------|----------|
| Swagger no carga | Verifica que la API este corriendo en puerto 5183 |
| Error de conexion MySQL | Verifica que MySQL este corriendo en localhost:3306 con root sin pass |
| Base de datos no existe | Ejecuta el script.sql en HeidiSQL o MySQL Workbench |
| Pedido queda en `EsperaConfirmacion` | La Cocina no esta corriendo |
| `503 Service Unavailable` | La Cocina no esta disponible |
| Error 400 con `errors` | FluentValidation rechazo los datos |
| `email` duplicado | Usa otro email, el campo es unico en la BD |

---

## 7. Resumen de Endpoints para Testing Rapido

```
1. GET  /api/pizzas                    -> Ver catalogo (nombres disponibles)
2. POST /api/clientes                  -> Registrar cliente (nombre, email, telefono, direccion)
3. GET  /api/clientes/{id}             -> Verificar datos del cliente por ID
4. GET  /api/clientes/email/{email}    -> Verificar datos del cliente por email
5. POST /api/pedidos                   -> Crear pedido (clienteId + items con pizzaNombre)
6. GET  /api/pedidos/{id}              -> Consultar estado del pedido
```
