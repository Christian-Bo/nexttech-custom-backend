# Módulos del backend

Los módulos iniciales son:

- **Auth:** Login de usuarios internos, cambio de contraseña y autorización relacionada con cuentas SQL Server.
- **OracleIntegration:** Casos de uso que requieren consultar compradores del Oracle central. No duplicar el usuario central.
- **Catalog:** Categorías, productos, variantes, atributos, imágenes y configuración administrable del catálogo.
- **Personalization:** Creación/edición/bloqueo de personalizaciones y validación de zonas/configuración JSON.
- **Cart:** Carrito activo por comprador, detalles, cantidades y personalizaciones asociadas.
- **Orders:** Checkout, creación de orden, snapshots históricos, tracking e historial de estados.
- **Payments:** Pago efectivo/tarjeta, integración con proveedor y persistencia de resultados no sensibles.
- **Delivery:** Órdenes disponibles, toma atómica, intentos de entrega y confirmación.
- **Notifications:** Confirmación de compra, pedido listo y entrega confirmada respetando preferencia Oracle.
- **Audit:** Registro de acciones importantes en BitacoraAuditoria.
- **Dashboard:** Consultas/agrupaciones para indicadores en tiempo real.