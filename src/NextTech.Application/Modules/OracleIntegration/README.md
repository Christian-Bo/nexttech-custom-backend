# Módulo OracleIntegration

Casos de uso que requieren consultar compradores del Oracle central. No duplicar el usuario central.

## Regla

No acceder directamente a detalles de infraestructura desde controllers. Definir el caso de uso en Application y usar contratos/interfaces cuando corresponda.
