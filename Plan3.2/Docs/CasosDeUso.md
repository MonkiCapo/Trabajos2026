# Casos de Uso — PizzeriaAPI
**Curso:** Computación — ET12 DE1

---

## 1. Listado General de Casos de Uso

### Actor: Cliente
| Código | Nombre | Descripción |
|--------|--------|-------------|
| **UC-01** | Registrar Cliente | El cliente introduce sus datos básicos para obtener su identificador del sistema. |
| **UC-02** | Consultar Pedido | El cliente monitorea el estado en tiempo real de su orden activa. |
| **UC-03** | Realizar Pedido | El cliente confirma su lista de compras, gatillando el flujo de negocio de la API. |

### Actor: Servicio de Cocina (Proceso Separado)
| Código | Nombre | Descripción |
|--------|--------|-------------|
| **UC-04** | Conectar y Leer Pedido | El proceso de la cocina asimila el JSON enviado por bytes a través del socket TCP. |
| **UC-05** | Notificar Preparado | La cocina informa mediante socket que finalizó la cocción simulada de la pizza. |

### Actor: Servicio de Reparto (Proceso Separado)
| Código | Nombre | Descripción |
|--------|--------|-------------|
| **UC-06** | Recibir Asignación de Envío | El reparto asimila los datos de entrega e inicia la simulación del transporte logístico. |
| **UC-07** | Notificar Entregado | El repartidor informa por socket TCP que las pizzas llegaron efectivamente a destino. |

---

## 2. Diagrama General de Casos de Uso

Este diagrama modela los límites de los módulos independientes conectados por la red y cómo se asocian a sus funciones:

```mermaid
flowchart TB
    subgraph "Sistema Backend (Pizzería Minimal API)"
        direction TB
        UC01["UC-01: Registrar Cliente"]
        UC02["UC-02: Consultar Pedido"]
        UC03["UC-03: Realizar Pedido"]
        UC04["UC-04: Conectar y Leer Pedido"]
        UC05["UC-05: Notificar Preparado"]
        UC06["UC-06: Recibir Asignación"]
        UC07["UC-07: Notificar Entregado"]
    end

    CL["Cliente (App C#)"]
    CO["Servicio Cocina (Socket)"]
    RE["Servicio Reparto (Socket)"]

    CL --> UC01
    CL --> UC02
    CL --> UC03
    CO --> UC04
    CO --> UC05
    RE --> UC06
    RE --> UC07


---

## 3. Diseño Jerárquico y Delegación de Responsabilidades

El sistema sigue una estructura de delegación en tres niveles. Cada nivel delega tareas al siguiente, recibiendo resultados de vuelta:

```mermaid
graph TD
    subgraph "Nivel 1: Cliente"
        CL["Cliente (App C#)"]
        CL -->|"UC-01: RegistrarCliente"| API
        CL -->|"UC-03: RealizarPedido"| API
        CL -->|"UC-02: ConsultarPedido"| API
    end

    subgraph "Nivel 2: Backend (Orquestador)"
        API["Backend Minimal API"]
        API -->|"Interno: Validar y persistir"| DB[(Base de Datos)]
        API -->|"UC-04: Delegar cocción"| CO
        API -->|"UC-06: Delegar reparto"| RE
    end

    subgraph "Nivel 3: Servicios Internos (Ejecutores)"
        CO["Servicio Cocina"]
        RE["Servicio Reparto"]
        CO -->|"UC-05: Notificar resultado"| API
        RE -->|"UC-07: Notificar resultado"| API
    end

    style CL fill:#e1f5fe
    style API fill:#fff3e0
    style DB fill:#f3e5f5
    style CO fill:#e8f5e9
    style RE fill:#e8f5e9
```

### Relación de delegación

| Nivel | Actor | Responsabilidad | Delega a | ¿Cómo? |
|-------|-------|----------------|----------|--------|
| **1** | Cliente | Iniciar el proceso, consultar resultados | Backend (API) | HTTP REST |
| **2** | Backend | Orquestar el flujo, validar, persistir | Cocina, Reparto | Socket TCP |
| **3** | Cocina | Ejecutar cocción simulada | — | — |
| **3** | Reparto | Ejecutar entrega simulada | — | — |

### Flujo de delegación por caso de uso

```
UC-03 (Realizar Pedido):
  Cliente ──HTTP──> Backend
                       ├── Validar y persistir pedido
                       ├── Socket ──> Cocina (UC-04)
                       │                 └── Socket ──> Backend (UC-05)
                       └── Socket ──> Reparto (UC-06)
                                       └── Socket ──> Backend (UC-07)

UC-02 (Consultar Pedido):
  Cliente ──HTTP──> Backend
                       └── Leer de BD y responder
```