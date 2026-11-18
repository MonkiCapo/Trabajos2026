# Pre-Requisitos — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Software Necesario

### 1.1 .NET SDK 10.0
El proyecto utiliza .NET 10.0. Verificar la instalación:

```bash
dotnet --version
```

Si no está instalado, descargar desde: https://dotnet.microsoft.com/download

### 1.2 MySQL Server 8.0+
El backend requiere una instancia de MySQL activa.

```bash
# Verificar conexión (ejemplo)
mysql -h localhost -P 3306 -u 5to_agbd -p
```

**Credenciales por defecto del proyecto:**
| Parámetro | Valor |
|-----------|-------|
| Server | localhost |
| Port | 3306 |
| Database | 5to_Pizzeria |
| User | 5to_agbd |
| Password | Trigg3rs! |

> **Nota:** Si usa otras credenciales, modificar la cadena de conexión en `appsettings.json` (o el fallback en `Program.cs`).

### 1.3 Editor de Código (Recomendado)
- Visual Studio 2022 17.x (con workload .NET Desktop)
- Visual Studio Code (con extensión C# Dev Kit)
- Rider (JetBrains)

---

## 2. Estructura del Proyecto

```
Plan3.2/
├── Docs/                          # Documentación técnica
│   ├── ArquitecturaDistribuida.md
│   ├── CasosDeUso.md
│   ├── ErroresAsync.md
│   ├── ExploracionAPI.md
│   ├── FallosYAcciones.md
│   ├── FlujoFuncional.md
│   ├── GuiaDefensa.md
│   ├── GuiaSwagger.md
│   ├── IntroduccionDistribuida.md
│   └── PreRequisitos.md           # Este documento
├── Relevamiento.md                # Relevamiento y diseño estructurado
└── src/
    ├── PizzeriaApp.slnx           # Archivo de solución
    ├── script.sql                 # Script de base de datos
    ├── Api.Pizzeria/              # Backend (Minimal API, HTTP)
    ├── Core.Pizzeria/             # Entidades, DTOs e interfaces de dominio
    ├── Dapper.Pizzeria/           # Implementación de repositorios (Dapper + MySQL)
    └── Consola.Cliente/           # Interfaz del cliente (HTTP)
```

---

## 3. Pasos de Ejecución

### Paso 1: Preparar la base de datos

Asegurarse de que MySQL esté ejecutándose. El sistema inicializa la base de datos automáticamente al iniciar la API (crea la base, tablas y datos semilla de `script.sql`, y aplica migraciones de columnas si la base ya existía).

```bash
# Si desea ejecutar manualmente el script:
mysql -u 5to_agbd -p < src/script.sql
```

### Paso 2: Compilar la solución

```bash
cd Plan3.2/src
dotnet restore
dotnet build
```

### Paso 3: Iniciar la API Backend

```bash
# Terminal 1: API
cd Plan3.2/src
dotnet run --project Api.Pizzeria
```

La API arranca en: `http://localhost:5183`
Swagger UI disponible en: `http://localhost:5183/swagger`

### Paso 4 (opcional): Iniciar la aplicación Cliente

```bash
# Terminal 2: Cliente
cd Plan3.2/src
dotnet run --project Consola.Cliente
```

> **Tip:** El flujo completo se puede probar solo con Swagger: registrar cliente, crear pedido y avanzar los estados con `PUT /api/pedidos/{id}/estado` (ver `GuiaSwagger.md`).

---

## 4. Orden de Ejecución Correcto

```
1. MySQL (debe estar corriendo)
2. Api.Pizzeria    ← Inicializa la base y levanta HTTP en el puerto 5183
3. Consola.Cliente ← Se conecta vía HTTP a la API (opcional; Swagger alcanza)
```

> **Error común:** Si MySQL no está corriendo, la API falla al inicializar la base de datos.

---

## 5. Puertos Utilizados

| Servicio | Puerto | Protocolo | Uso |
|----------|--------|-----------|-----|
| Api.Pizzeria (HTTP) | 5183 | HTTP | Endpoints REST + Swagger |
| MySQL | 3306 | MySQL/TCP | Persistencia de datos |

---

## 6. Dependencias NuGet del Backend

| Paquete | Uso |
|---------|-----|
| `Microsoft.AspNetCore.OpenApi` | Documentación OpenAPI |
| `Swashbuckle.AspNetCore` | UI de Swagger |
| `MySqlConnector` | Conector MySQL para .NET |
| `Dapper` | Micro-ORM para queries SQL |
| `FluentValidation` (+ `FluentValidation.DependencyInjectionExtensions`) | Validación de DTOs |

---

## 7. Solución de Problemas Comunes

### "No se pudo conectar a la API"
- Verificar que `Api.Pizzeria` esté corriendo
- Verificar que el puerto 5183 no esté ocupado

### "Error de conexión a MySQL"
- Verificar que MySQL esté corriendo en el puerto 3306
- Verificar credenciales en `appsettings.json` (o el fallback en `Program.cs`)
- Verificar que el usuario tenga permisos para crear bases de datos

### "El pedido queda en EsperaConfirmacion"
- Es el estado inicial por diseño: avanzarlo con `PUT /api/pedidos/{id}/estado` desde Swagger (rol administrador)

### "script.sql no encontrado"
- El sistema busca el script en múltiples ubicaciones
- Asegurar que `script.sql` esté en `Plan3.2/src/`
