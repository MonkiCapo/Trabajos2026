# Introducción a la Programación Distribuida

**Proyecto:** PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. ¿Qué es la Programación Distribuida?

Es un paradigma donde un sistema se compone de **múltiples procesos independientes** que se ejecutan en **distintos nodos de red** y se coordinan intercambiando mensajes. A diferencia de una aplicación monolítica (todo en un mismo ejecutable), un sistema distribuido reparte responsabilidades entre distintos servicios que se comunican a través de protocolos de red.

### Características clave
- **Concurrencia:** Los componentes ejecutan tareas en paralelo.
- **Inexistencia de reloj global:** Cada nodo tiene su propio reloj; no hay un punto de sincronización único.
- **Fallos independientes:** Un nodo puede caerse sin afectar necesariamente a los demás.
- **Comunicación por mensajes:** Los componentes intercambian datos mediante protocolos estandarizados (HTTP, TCP/UDP, etc.).

---

## 2. Modelo Cliente-Servidor

El modelo **cliente-servidor** es el paradigma central de este proyecto:

| Rol | Función | Ejemplo en el proyecto |
|-----|---------|----------------------|
| **Cliente** | Inicia la comunicación, solicita servicios. | App C# que envía POST/GET HTTP al backend. |
| **Servidor** | Escucha peticiones, las procesa y responde. | Backend (Minimal API) que orquesta pedidos. |
| **Servicio interno** | Proceso auxiliar que el servidor contacta. | Servicio de Cocina, Servicio de Reparto (vía sockets). |

### Flujo típico
```
Cliente → (HTTP) → Backend → (Socket) → Cocina
                                    → Reparto
```

---

## 3. Comparación con otros modelos

| Modelo | Descripción | Ventajas | Desventajas |
|--------|-------------|----------|-------------|
| **Monolítico** | Todo el código en un solo proceso. | Simplicidad, debugging directo. | Escalabilidad limitada, un fallo lo detiene todo. |
| **Cliente-Servidor** | Un servidor central atiende múltiples clientes. | Separación de responsabilidades, escalable. | Punto único de fallo (el servidor). |
| **Peer-to-Peer (P2P)** | Todos los nodos son pares (no hay servidor fijo). | Alta tolerancia a fallos, descentralizado. | Complejidad de coordinación, seguridad. |
| **Microservicios** | División del servidor en servicios pequeños e independientes. | Escalabilidad granular, despliegue independiente. | Mayor complejidad de red y orquestación. |

**Nuestro caso** adopta un híbrido **cliente-servidor con servicios internos**: el backend actúa como servidor central frente al cliente, pero a su vez es cliente de los servicios de Cocina y Reparto mediante sockets TCP.

---

## 4. De local a distribuido

Para transformar una aplicación local en distribuida, se aplican los siguientes cambios:

| Aspecto Local | Aspecto Distribuido (nuestro sistema) |
|---------------|---------------------------------------|
| Llamadas a función directas | Mensajes HTTP (cliente → API) y Socket TCP (API → Cocina/Reparto) |
| Datos en memoria compartida | Cada servicio tiene su propio estado; se comunican por mensajes |
| Un solo flujo de ejecución | Múltiples procesos concurrentes (API, Cocina, Reparto) |
| Errores como excepciones locales | Fallos de red, timeouts, reintentos |

### Ejemplo concreto
- **Local:** La lógica de cocina y reparto estaría dentro del mismo ejecutable, acoplada.
- **Distribuido:** La Cocina es un proceso separado que se ejecuta en otra máquina (o puerto) y se comunica por socket. Si la Cocina se cae, el Backend puede seguir funcionando y rechazar pedidos con un mensaje de error adecuado.

---

## 5. Caso de uso: PizzeriaAPI

El sistema modela una pizzería con tres procesos autónomos:

```
┌──────────────┐     HTTP      ┌──────────────────┐     Socket TCP     ┌──────────────────┐
│  Cliente      │ ──────────>  │   Backend API    │ ────────────────>  │  Servicio Cocina  │
│  (App C#)     │ <────────── │   (Minimal API)  │ <──────────────── │  (C# Process)     │
└──────────────┘              └──────────────────┘                    └──────────────────┘
                                      │
                                      │ Socket TCP
                                      │
                               ┌──────┴───────┐
                               │    Servicio   │
                               │    Reparto    │
                               └──────────────┘
```

**Flujo de trabajo:**
1. El cliente envía un pedido por HTTP POST.
2. El backend valida, persiste y contacta a la Cocina por socket TCP.
3. La Cocina prepara (delay simulado) y notifica cuando termina.
4. El backend asigna el reparto por socket TCP.
5. El Reparto entrega (delay simulado) y notifica.
6. El cliente puede consultar el estado en cualquier momento por HTTP GET.

---

## 6. Conclusión

Este proyecto aplica los conceptos fundamentales de programación distribuida: separación de procesos, comunicación por mensajes (REST + Sockets), manejo de concurrencia y tratamiento de fallos de red. Es un ejemplo didáctico de cómo un sistema real desacopla responsabilidades para ganar escalabilidad y resiliencia.
