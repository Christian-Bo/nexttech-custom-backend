# Deploy

## Desarrollo con Docker

Desde la raíz:

```bash
docker compose --env-file .env -f deploy/docker-compose.yml up -d --build
```

La imagen oficial de SQL Server **no ejecuta automáticamente** el `.sql` de este repositorio al iniciar. Después de que SQL Server esté healthy, ejecutar `database/sqlserver/NextTechCustomDB_DBeaver_Nickname.sql` desde DBeaver o mediante una herramienta SQL autorizada.

Oracle es externo y no forma parte del compose.
