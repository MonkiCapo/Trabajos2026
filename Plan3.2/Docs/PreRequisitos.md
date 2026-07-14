# Pre-Requisitos — PizzeriaAPI

**Proyecto:** "Tu app pide una pizza... y la API se la entrega"
**Curso:** Computación — ET12 DE1

---

## 1. Software Necesario

### 1.1 .NET SDK 10.0
El proyecto utiliza .NET 10.0 (preview/latest). Verificar la instalación:

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

> **Nota:** Si usa otras credenciales, modificar la cadena de conexión en `appsettings.json` o en el código fuente de `Program.cs` y `PedidoService.cs`.

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
│   ├── FlujoFuncional.md          # Este documento
│   ├── GuiaDefensa.md
│   ├── GuiaSwagger.md
│   ├── IntroduccionDistribuida.md
│   └── PreRequisitos.md           # Este documento
├── Relevamiento.md                # Relevamiento y diseño estructurado
└── src/
    ├── PizzeriaApp.slnx           # Archivo de solución
    ├── script.sql                 # Script de base de datos
    ├── Api.Pizzeria/              # Backend (Minimal API + Socket Server)
    ├── Consola.Cliente/           # Interfaz del cliente (HTTP)
    ├── Consola.Cocina/            # Servicio de cocina (TCP Socket)
    └── Consola.Reparto/           # Servicio de reparto (TCP Socket)
```

---

## 3. Pasos de Ejecución

### Paso 1: Preparar la base de datos

Asegurarse de que MySQL esté ejecutándose. El sistema inicializa la base de datos automáticamente al iniciar la API (usa `script.sql`).

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

### Paso 3: Iniciar los servidores Socket (Cocina y Reparto)

Abrir **tres terminales separadas** y ejecutar en cada una:

```bash
# Terminal 1: Cocina
cd Plan3.2/src
dotnet run --project Consola.Cocina

# Terminal 2: Reparto
cd Plan3.2/src
dotnet run --project Consola.Reparto
```

> **Importante:** Cocina y Reparto deben estar ejecutándose **antes** de la API, ya que el servidor socket del backend espera conexiones entrantes.

### Paso 4: Iniciar la API Backend

```bash
# Terminal 3: API
cd Plan3.2/src
dotnet run --project Api.Pizzeria
```

La API arranca en: `http://localhost:5183`
Swagger UI disponible en: `http://localhost:5183/swagger`

### Paso 5: Iniciar la aplicación Cliente

```bash
# Terminal 4: Cliente
cd Plan3.2/src
dotnet run --project Consola.Cliente
```

---

## 4. Orden de Ejecución Correcto

```
1. MySQL (debe estar corriendo)
2. Consola.Cocina  ← Se conecta al puerto 7000
3. Consola.Reparto ← Se conecta al puerto 7000
4. Api.Pizzeria    ← Inicia escuchador socket en puerto 7000 + HTTP en 5183
5. Consola.Cliente ← Se conecta vía HTTP a la API
```

> **Error común:** Si se inicia la API antes que Cocina/Reparto, el primer pedido fallará con error 503 (Cocina no disponible).

---

## 5. Puertos Utilizados

| Servicio | Puerto | Protocolo | Uso |
|----------|--------|-----------|-----|
| Api.Pizzeria (HTTP) | 5183 | HTTP | Endpoints REST + Swagger |
| Socket Server | 7000 | TCP | Comunicación con Cocina y Reparto |

---

## 6. Dependencias NuGet del Backend

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.AspNetCore.OpenApi` | — | Documentación Swagger |
| `Swashbuckle.AspNetCore` | — | UI de Swagger |
| `MySqlConnector` | — | Conector MySQL para .NET |
| `Dapper` | — | ORM ligero para queries SQL |
| `FluentValidation` | — | Validación de DTOs |

---

## 7. Solución de Problemas Comunes

### "No se pudo conectar a la API"
- Verificar que `Api.Pizzeria` esté corriendo
- Verificar que el puerto 5183 no esté ocupado

### "Cocina no disponible (503)"
- Verificar que `Consola.Cocina` esté corriendo
- Verificar que el puerto 7000 no esté ocupado
- El socket server del backend debe aceptar conexiones entrantes

### "Error de conexión a MySQL"
- Verificar que MySQL esté corriendo en el puerto 3306
- Verificar credenciales en `Program.cs` o `appsettings.json`
- Verificar que el usuario tenga permisos para crear bases de datos

### "script.sql no encontrado"
- El sistema busca el script en múltiples ubicaciones
- Asegurar que `script.sql` esté en `Plan3.2/src/`
