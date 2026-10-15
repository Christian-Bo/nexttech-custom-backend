# Módulo Orders

Checkout, creación de orden, snapshots históricos, tracking e historial de estados.

Tracking en vivo: `src/NextTech.Infrastructure/Realtime/README.md` (`/hubs/tracking`).

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
