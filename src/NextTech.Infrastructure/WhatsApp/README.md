# WhatsApp

Integración saliente de NextTech Custom con la API privada de WhatsApp basada en Baileys.

## Responsabilidad

La aplicación usa únicamente estos endpoints del servicio externo:

- `POST /messages/text`: credencial QR inicial después del registro cuando el comprador habilitó WhatsApp.
- `POST /messages/media`: credencial digital PDF después de emitirla cuando el comprador habilitó WhatsApp.

NextTech Custom no expone endpoints nuevos para WhatsApp; las llamadas se realizan desde los casos de uso existentes.

## Configuración

Sección ASP.NET Core `WhatsApp`:

- `Enabled`
- `BaseUrl`
- `ApiKey`
- `ApiKeyHeader`
- `TimeoutSeconds`

En despliegue con `deploy/docker-compose.yml` se alimentan mediante:

- `WHATSAPP_ENABLED`
- `WHATSAPP_API_BASE_URL`
- `WHATSAPP_API_KEY`
- `WHATSAPP_API_KEY_HEADER`
- `WHATSAPP_API_TIMEOUT_SECONDS`

La API Key debe mantenerse fuera del repositorio.

## Comportamiento

El envío es una notificación auxiliar: un fallo temporal del proveedor se registra sin exponer teléfono, QR, PDF ni API Key y no revierte la operación principal ya completada. La cancelación solicitada por el cliente sí se propaga.

El cliente limita los adjuntos a 15 MB antes de convertirlos a Base64, consistente con el servicio de WhatsApp desplegado.
