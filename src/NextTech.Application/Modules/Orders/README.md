# Módulo Orders

Checkout, creación de orden, snapshots históricos, tracking e historial de estados.

Tracking en vivo: `src/NextTech.Infrastructure/Realtime/README.md` (`/hubs/tracking`).

Hoja de impresión Lado A/B (supervisor):

- Toda la cola en elaboración: `GET /api/supervisor/orders/print-queue`
- Varios pedidos a la vez: `POST /api/supervisor/orders/print` `{ "codigos": ["ORD-1", "ORD-2"] }`
- Un pedido o reimpresión (corte malogrado): `GET /api/supervisor/orders/{codigo}/print`

La reimpresión no cambia el estado del pedido: las imágenes siguen en el archivo y se puede volver a generar el PDF las veces que haga falta, aunque el pedido ya esté listo o entregado. Si salieron mal varios, `POST /print` con esos códigos.

Papel A4 21 × 29.7 cm (normal o fotográfico). Cada Lado A/B es un círculo de 1.6 in (4.06 cm). ~2.6 cm entre A y B, ~3.2 cm entre un llavero y el de abajo para la tijera. 3 llaveros por página. Imprimir al 100%; la barra negra debe medir 4.06 cm con una regla.

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
