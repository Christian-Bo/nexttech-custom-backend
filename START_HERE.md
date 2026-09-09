# EMPEZAR AQUÍ - Backend NextTech Custom

Este ZIP ya contiene la **base técnica necesaria para comenzar a programar**.
No contiene el proyecto terminado ni la lógica completa de negocio.

## Ya está preparado

- ASP.NET Core 8 Web API.
- Arquitectura monolítica modular por capas.
- Proyectos `Api`, `Application`, `Domain` e `Infrastructure`.
- Entity Framework Core para SQL Server.
- Proveedor EF Core oficial de Oracle.
- `NextTechDbContext` con las 34 tablas actuales de `NextTechCustomDB`.
- Configuración de columnas/tablas SQL Server.
- `OracleDbContext` de solo lectura.
- Modelo mínimo de `TIENDA_APP.USUARIO`.
- `ICompradorCentralReader` y su implementación Oracle.
- JWT configurado a nivel de infraestructura.
- Swagger con soporte Bearer.
- CORS para Nuxt local.
- `ProblemDetails` / manejo base de errores.
- `/health/live`.
- `/health/ready` verificando SQL Server + Oracle.
- Dockerfile y Docker Compose.
- GitHub Actions CI.
- Proyectos de pruebas xUnit funcionales.
- Script SQL oficial de `NextTechCustomDB`.

## Qué debes configurar antes de ejecutar

### Oracle

`src/NextTech.Api/appsettings.json` ya tiene:

```text
User Id=TIENDA_APP;
Data Source=www.server.daossystem.pro:5626/XEPDB1;
```

Solo reemplaza:

```text
Password=XXXX
```

por la contraseña real.

### SQL Server

En `appsettings.json` reemplaza:

```text
Password=REEMPLAZAR_SQLSERVER
```

por la contraseña de tu SQL Server local, o usa Docker.

## Ejecutar sin Docker

Desde la raíz:

```bash
dotnet restore NextTech.sln
dotnet build NextTech.sln
dotnet test NextTech.sln
dotnet run --project src/NextTech.Api/NextTech.Api.csproj
```

Después:

```text
Swagger:
http://localhost:8080/swagger

API viva:
http://localhost:8080/health/live

Bases disponibles:
http://localhost:8080/health/ready
```

`/health/live` confirma que la API está funcionando.

`/health/ready` intenta abrir una conexión real contra:

- SQL Server / NextTechCustomDB
- Oracle / TIENDA_APP

Si una cadena o contraseña está incorrecta, el endpoint mostrará qué conexión
está fallando.

## Importante: SQL Server es Database First

La estructura oficial sigue siendo:

```text
database/sqlserver/NextTechCustomDB_DBeaver_Nickname.sql
```

Las entidades y configuraciones incluidas representan esa estructura actual.

No utilizar:

```csharp
Database.EnsureCreated()
Database.EnsureDeleted()
```

y no generar migraciones que alteren la DB sin acuerdo del equipo.

Cuando la DB cambie oficialmente, primero se actualiza el script y después el
mapeo EF correspondiente.

## Oracle es de solo lectura

NextTech no crea ni modifica el esquema Oracle.

`OracleDbContext.SaveChanges()` está bloqueado intencionalmente.

La integración inicial disponible es:

```csharp
ICompradorCentralReader
```

para buscar por:

- ID
- nickname
- correo

Los casos de uso del backend decidirán cuándo utilizar esa información.

## Qué falta programar

Esto se dejó intencionalmente sin terminar:

- login real y emisión de JWT;
- PasswordHasher de usuarios internos;
- CRUD de catálogo;
- carrito;
- editor/persistencia de personalización;
- checkout;
- pagos;
- órdenes;
- transiciones de estados;
- entrega;
- notificaciones;
- auditoría;
- dashboard;
- PDF;
- QR;
- SignalR;
- integración Recurrente;
- correo/WhatsApp;
- validadores de cada caso de uso.

Esas son las tareas que deben desarrollar tus compañeros sobre esta base.
