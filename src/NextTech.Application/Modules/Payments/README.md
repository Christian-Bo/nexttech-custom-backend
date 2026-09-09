# Módulo Payments

Pago efectivo/tarjeta, integración con proveedor y persistencia de resultados no sensibles.

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
