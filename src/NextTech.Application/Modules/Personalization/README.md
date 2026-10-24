# Módulo Personalization

Creación/edición/bloqueo de personalizaciones y validación de zonas/configuración JSON.

El destino del chip NFC (`nfcUrl`) es opcional en `POST /api/personalizations`. Si viene, se guarda en `ConfiguracionJson`. Contrato: `src/NextTech.Application/Modules/Nfc/README.md`.

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
