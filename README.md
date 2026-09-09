# NextTech Custom Backend

Backend inicial del proyecto universitario **NextTech Custom**.

## Arquitectura

**Monolito modular** con ASP.NET Core 8:

```text
NextTech.Api
        ↓
NextTech.Application
        ↓
NextTech.Domain

NextTech.Infrastructure
   ├── SQL Server / NextTechCustomDB
   ├── Oracle central
   ├── autenticación
   └── integraciones externas
```

## Empieza aquí

Lee primero:

```text
START_HERE.md
```

El repositorio ya tiene las conexiones, DbContexts, EF Core, Oracle, JWT,
Swagger, health checks, CI, Docker y la estructura base para comenzar a
implementar los módulos.

## Bases

### SQL Server

`NextTechCustomDB` es la base propia de la tienda. El script oficial está en:

```text
database/sqlserver/NextTechCustomDB_DBeaver_Nickname.sql
```

### Oracle

La cuenta central se consulta desde Oracle y no se duplica en SQL Server.

```text
Oracle.USUARIO.ID_USUARIO -> IdCompradorExterno
Oracle.USUARIO.NICKNAME    -> NicknameCompradorAplicado
Oracle.USUARIO.CORREO      -> CorreoCompradorAplicado
Oracle.USUARIO.TELEFONO    -> TelefonoCompradorAplicado
```

Oracle está configurado como **solo lectura** desde NextTech.

## Git

```text
feature/* -> develop -> main
```

- `develop` como Default Branch.
- PR obligatorio.
- CI obligatorio.
- 1 aprobación recomendada.
- Sin push directo a `develop` o `main`.

## Comandos

```bash
dotnet restore NextTech.sln
dotnet build NextTech.sln
dotnet test NextTech.sln
dotnet run --project src/NextTech.Api/NextTech.Api.csproj
```
