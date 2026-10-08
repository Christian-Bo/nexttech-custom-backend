# Deploy

## Desarrollo con Docker

Desde la raíz:

```bash
docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
```

La imagen oficial de SQL Server **no ejecuta automáticamente** el `.sql` de este repositorio al iniciar. Después de que SQL Server esté healthy, ejecutar `database/sqlserver/NextTechCustomDB_DBeaver_Nickname.sql` desde DBeaver o mediante una herramienta SQL autorizada.

Oracle es externo y no forma parte del compose.

## WhatsApp

La integración con la API Baileys se configura con las variables documentadas en `deploy/whatsapp.env.example`.

En el servidor de NextTech Custom configure como mínimo:

```text
WHATSAPP_ENABLED=true
WHATSAPP_API_BASE_URL=https://api-whatsapp.34-174-37-7.sslip.io
WHATSAPP_API_KEY=<API_KEY_FINAL>
WHATSAPP_API_KEY_HEADER=X-API-Key
WHATSAPP_API_TIMEOUT_SECONDS=30
```

`deploy/docker-compose.yml` las transforma a la sección `WhatsApp` de ASP.NET Core. No agregue la API Key real al repositorio.
