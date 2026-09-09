# NextTech Backend

API de **NextTech Custom** construida como **monolito modular** con ASP.NET Core 8.

## Arquitectura

```text
NextTech.Api
    ↓
NextTech.Application
    ↓
NextTech.Domain

NextTech.Infrastructure
    ↘ SQL Server: NextTechCustomDB
    ↘ Oracle: usuarios/compradores centrales
    ↘ Pagos / correo / WhatsApp / PDF / QR
```

El backend es **una sola API desplegable**, pero el código está separado por módulos funcionales.

## Bases de datos

- **SQL Server / NextTechCustomDB:** catálogo, carrito, personalización, órdenes, pagos, entregas, notificaciones, auditoría y usuarios internos.
- **Oracle central:** compradores, autenticación de compradores, QR del comprador, preferencias de notificación y datos centrales.

Nunca se debe crear una FK física entre SQL Server y Oracle. En SQL Server se utiliza `IdCompradorExterno`.

## Primeros pasos

1. Instalar .NET SDK 8.
2. Copiar `.env.example` a un archivo local de variables de entorno o configurar User Secrets.
3. Levantar SQL Server o conectarse a la instancia de desarrollo.
4. Ejecutar `database/sqlserver/NextTechCustomDB_DBeaver_Nickname.sql` desde DBeaver.
5. Configurar la conexión Oracle sin subir credenciales al repositorio.
6. Ejecutar:
   ```bash
   dotnet restore NextTech.sln
   dotnet build NextTech.sln
   dotnet test NextTech.sln
   dotnet run --project src/NextTech.Api/NextTech.Api.csproj
   ```
7. Verificar:
   ```text
   GET /health
   ```

## Flujo Git

```text
feature/* ──PR──> develop ──PR de entrega──> main
```

- `develop` debe configurarse como rama predeterminada.
- No hacer push directo a `develop` ni a `main`.
- Todo PR debe pasar CI.
- Recomendado: 1 aprobación + conversaciones resueltas + Squash Merge.

Ver `docs/WORKFLOW_GIT.md`.

## Reglas importantes

- No almacenar contraseñas en texto plano.
- No duplicar compradores Oracle en SQL Server.
- No agregar lógica de negocio compleja mediante triggers.
- Los cambios de estado, pagos, checkout y toma de pedidos deben ejecutarse con transacciones cuando corresponda.
- Las reglas completas de integración con DB están en `docs/database/Guia_Backend_NextTechCustomDB.docx`.

## Módulos

- Auth
- OracleIntegration
- Catalog
- Personalization
- Cart
- Orders
- Payments
- Delivery
- Notifications
- Audit
- Dashboard

Cada módulo contiene un `README.md` con su responsabilidad inicial.
