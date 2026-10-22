# Módulo Orders

Checkout, creación de orden, snapshots históricos, tracking e historial de estados.

Tracking en vivo: `src/NextTech.Infrastructure/Realtime/README.md` (`/hubs/tracking`).

Hoja de impresión Lado A/B (supervisor). Solo las fotos en círculo de 1.6 in, sin textos. Papel por defecto **10×15 cm** (15 cm de ancho × 10 cm de alto), el fotográfico del taller.

- Toda la cola: `GET /api/supervisor/orders/print-queue?papel=10x15`
- Lote: `POST /api/supervisor/orders/print` `{ "codigos": ["ORD-1", "ORD-2"], "papel": "10x15" }`
- Uno / reimpresión: `GET /api/supervisor/orders/{codigo}/print?papel=10x15`

`papel` (si se omite, `10x15`): `10x15`, `13x18`, `15x21`, `20x25`, `4x6`, `5x7`, `8x10`, `a6`, `a5`, `a4`, `letter`. Solo las fotos; 1 cm entre A y B y entre llaveros para cortar. Imprimir al 100%; el PDF ya mide la hoja, no ajustar a la página.

La reimpresión no cambia el estado del pedido.

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
