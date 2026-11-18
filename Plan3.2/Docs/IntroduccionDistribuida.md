# Introducción a la Programación Distribuida

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. ¿Qué es la Programación Distribuida?

Es un paradigma donde un sistema se compone de **múltiples procesos independientes** que se ejecutan en **distintos nodos de red** y se coordinan intercambiando mensajes. A diferencia de una aplicación monolítica (todo en un mismo ejecutable), un sistema distribuido reparte responsabilidades entre distintos componentes que se comunican a través de protocolos de red.

### Características clave
- **Concurrencia:** Los componentes ejecutan tareas en paralelo.
- **Inexistencia de reloj global:** Cada nodo tiene su propio reloj; no hay un punto de sincronización único.
- **Fallos independientes:** Un nodo puede caerse sin afectar necesariamente a los demás.
- **Comunicación por mensajes:** Los componentes intercambian datos mediante protocolos estandarizados (HTTP, SQL/TCP, etc.).

---

## 2. Modelo Cliente-Servidor

El modelo **cliente-servidor** es el paradigma central de este proyecto:

| Rol | Función | Ejemplo en el proyecto |
|-----|---------|----------------------|
| **Cliente** | Inicia la comunicación, solicita servicios. | App C# (Consola.Cliente) que envía POST/GET HTTP al backend. |
| **Servidor** | Escucha peticiones, las procesa y responde. | Backend (Api.Pizzeria, Minimal API) que valida, aplica reglas de negocio y persiste. |
| **Cliente de administración** | Usa los mismos servicios con permisos de gestión. | Swagger UI en el navegador, actuando como administrador. |
| **Nodo de datos** | Persiste y sirve la información al servidor. | MySQL (5to_Pizzeria), accedido con Dapper. |

### Flujo típico
```
Consola.Cliente ──► (HTTP/JSON) ──► Api.Pizzeria ──► (SQL/TCP 3306) ──► MySQL
Swagger (Admin) ──► (HTTP/JSON) ──► Api.Pizzeria ──► (SQL/TCP 3306) ──► MySQL
```

---

## 3. Comparación con otros modelos

| Modelo | Descripción | Ventajas | Desventajas |
|--------|-------------|----------|-------------|
| **Monolítico** | Todo el código en un solo proceso. | Simplicidad, debugging directo. | Escalabilidad limitada, un fallo lo detiene todo. |
| **Cliente-Servidor** | Un servidor central atiende múltiples clientes. | Separación de responsabilidades, escalable. | Punto único de fallo (el servidor). |
| **Peer-to-Peer (P2P)** | Todos los nodos son pares (no hay servidor fijo). | Alta tolerancia a fallos, descentralizado. | Complejidad de coordinación, seguridad. |
| **Microservicios** | División del servidor en servicios pequeños e independientes. | Escalabilidad granular, despliegue independiente. | Mayor complejidad de red y orquestación. |

**Nuestro caso** adopta el modelo **cliente-servidor clásico**: la API es el servidor central frente a los clientes (consola y navegador) y, a su vez, es cliente del nodo de base de datos MySQL. La base de datos y la API pueden vivir en máquinas distintas: la distribución está en la comunicación por red, no en la cantidad de procesos propios.

---

## 4. De local a distribuido

Para transformar una aplicación local en distribuida, se aplican los siguientes cambios:

| Aspecto Local | Aspecto Distribuido (nuestro sistema) |
|---------------|---------------------------------------|
| Llamadas a función directas | Mensajes HTTP (cliente → API) y consultas SQL (API → MySQL) |
| Datos en memoria compartida | Cada proceso tiene su propio estado; se comunican por mensajes |
| Un solo flujo de ejecución | Múltiples procesos concurrentes (Cliente, API, MySQL) |
| Errores como excepciones locales | Fallos de red, timeouts, códigos de error HTTP |

### Ejemplo concreto
- **Local:** la lógica de validación y persistencia estaría dentro del mismo ejecutable que la interfaz, acoplada.
- **Distribuido:** la API es un proceso separado al que el cliente le habla por HTTP. Si la API se cae, la consola detecta el fallo de conexión y lo informa; si MySQL se cae, la API responde 500 sin corromper datos (gracias a las transacciones).

---

## 5. Caso de uso: PizzeriaAPI

El sistema modela una pizzería con procesos autónomos que se coordinan por red:

```
┌──────────────┐     HTTP      ┌──────────────────┐      SQL       ┌──────────────┐
│  Consola.    │ ───────────>  │   Api.Pizzeria   │ ────────────>  │    MySQL     │
│  Cliente     │ <───────────  │  (Minimal API)   │ <────────────  │ (5to_Pizzeria)│
└──────────────┘               └──────────────────┘                └──────────────┘
                                      ▲
┌──────────────┐     HTTP             │
│  Swagger UI  │ ─────────────────────┘
│ (Administr.) │
└──────────────┘
```

**Flujo de trabajo:**
1. El cliente envía un pedido por `POST /api/pedidos` (HTTP).
2. La API valida, abre una transacción, descuenta stock y persiste el pedido en estado `EsperaConfirmacion`.
3. El administrador avanza el ciclo de vida con `PUT /api/pedidos/{id}/estado`: `EnPreparacion` → `EnViaje` → `Entregado` (simulando cocina y reparto).
4. El cliente consulta el estado en cualquier momento por `GET /api/pedidos/{id}` (polling cada 2 segundos).
5. La API responde con códigos semánticos: 201 creado, 400 datos inválidos, 404 inexistente, 409 conflicto.

---

## 6. Conclusión

Este proyecto aplica los conceptos fundamentales de programación distribuida: separación de procesos, comunicación por mensajes (REST + SQL sobre red), manejo de concurrencia (transacciones con stock atómico) y tratamiento de fallos de red (timeouts, códigos HTTP semánticos, logging). Es un ejemplo didáctico de cómo un sistema real desacopla la interfaz, la lógica de negocio y los datos, ganando escalabilidad y resiliencia.
