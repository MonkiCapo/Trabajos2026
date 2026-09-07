# Introducción a la Arquitectura de APIs REST

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. ¿Qué es una API REST?

Una **API REST** (Representational State Transfer) es un estilo de arquitectura donde un sistema expone sus operaciones como **recursos** que el cliente manipula mediante **HTTP**. Cada recurso tiene una URL y se opera con los verbos HTTP (`GET`, `POST`, `PATCH`, `PUT`, `DELETE`).

### Características clave
- **Stateless:** Cada petición HTTP es independiente; el servidor no guarda estado de sesión del cliente.
- **Recursos identificados por URL:** `/clientes`, `/pedidos`, `/pizzas`.
- **Verbos HTTP semánticos:** `GET` (leer), `POST` (crear), `PATCH` (actualizar parcialmente), `DELETE` (eliminar).
- **JSON como formato de intercambio.**
- **Respuestas con códigos de estado estándar** (`200`, `201`, `400`, `404`, `500`).

---

## 2. Modelo Cliente-Servidor

El modelo **cliente-servidor** es el paradigma central de este proyecto:

| Rol | Función | Ejemplo en el proyecto |
|-----|---------|----------------------|
| **Cliente** | Inicia la comunicación, solicita servicios. | App C# que envía POST/GET/PATCH HTTP al backend. |
| **Servidor** | Escucha peticiones, las procesa y responde. | Backend (Minimal API) que gestiona pedidos. |

### Flujo típico
```
Cliente → (HTTP POST/GET/PATCH) → Backend → (SQL) → MySQL
```

---

## 3. Comparación con otros modelos

| Modelo | Descripción | Ventajas | Desventajas |
|--------|-------------|----------|-------------|
| **Monolítico** | Todo el código en un solo proceso. | Simplicidad, debugging directo, menos fallos de red. | Escalabilidad vertical limitada. |
| **Cliente-Servidor** | Un servidor central atiende múltiples clientes. | Separación de responsabilidades, escalable. | Punto único de fallo (el servidor). |
| **Distribuido (microservicios/sockets)** | Múltiples procesos que se comunican por red. | Escalabilidad granular. | Mayor complejidad de red y orquestación. |
| **Peer-to-Peer (P2P)** | Todos los nodos son pares. | Alta tolerancia a fallos. | Complejidad de coordinación. |

**Nuestro caso** adopta el modelo **monolítico cliente-servidor**: un único backend REST concentra la lógica, la validación y la persistencia. No hay procesos externos (cocina/reparto) con los que coordinar fallos de red; toda la complejidad de comunicación queda reducida al canal HTTP.

---

## 4. De local a API REST

Para transformar una aplicación local en una API REST, se aplican los siguientes cambios:

| Aspecto Local | Aspecto REST (nuestro sistema) |
|---------------|--------------------------------|
| Llamadas a función directas | Mensajes HTTP (cliente → API): `POST`, `GET`, `PATCH` |
| Datos en memoria compartida | La API persiste en MySQL; el cliente consulta por HTTP |
| Un solo flujo de ejecución | Endpoints que se ejecutan de forma independiente |
| Errores como excepciones locales | Códigos de estado HTTP (400, 404, 500) |

### Ejemplo concreto
- **Local:** La lógica de pedidos estaría acoplada al código del cliente.
- **REST:** El cliente envía `POST /api/pedidos`; la API valida, persiste y responde `201`. El estado del pedido se gestiona con `PATCH /api/pedidos/{id}/estado` desde cualquier consumidor HTTP.

---

## 5. Caso de uso: PizzeriaAPI

El sistema modela una pizzería con un **backend central** y consumidores HTTP:

```
┌──────────────┐     HTTP      ┌──────────────────┐     SQL      ┌──────────────┐
│  Cliente      │ ──────────>  │   Backend API    │ ───────────>  │   MySQL      │
│  (App C# /    │ <──────────  │   (Minimal API)  │ <───────────  │  (Persistencia)│
│   Scalar)    │              └──────────────────┘               └──────────────┘
└──────────────┘
```

### Estado del pedido (máquina de estados gestionada por la API)
```
EsperaConfirmacion --PATCH--> EnPreparacion --PATCH--> EnViaje --PATCH--> Entregado
        \--PATCH--> Cancelado
```

**Flujo de trabajo:**
1. El cliente envía un pedido por HTTP POST.
2. El backend valida y persiste el pedido en `EsperaConfirmacion`.
3. Un administrador avanza el estado con PATCH: `EnPreparacion` → `EnViaje` → `Entregado`.
4. El cliente puede consultar el estado en cualquier momento por HTTP GET.

---

## 6. Conclusión

Este proyecto aplica los conceptos fundamentales de APIs REST y arquitectura cliente-servidor monolítica: recursos HTTP, estados semánticos, persistencia centralizada y validación. Al eliminar los sockets TCP, la comunicación se simplifica a un único canal síncrono y robusto, más fácil de entender, probar y escalar.