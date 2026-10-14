# Módulo Personalization

Creación/edición/bloqueo de personalizaciones y validación de zonas/configuración JSON.

El destino del chip NFC (`nfcUrl`) se pide en `POST /api/personalizations` y se guarda dentro de `ConfiguracionJson`. Contrato completo: `src/NextTech.Application/Modules/Nfc/README.md`.

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
