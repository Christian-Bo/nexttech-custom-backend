# Tracking en vivo (SignalR)

El comprador carga el pedido con HTTP y se suscribe a cambios de estado. Sin GPS y sin cambios de esquema.

## Contrato frontend (Nuxt, JWT de comprador)

1. Carga inicial (igual que ahora):

`GET /api/orders/{codigo}/tracking`

2. Conectar al hub (WebSocket no manda `Authorization`; el token va en query):

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${apiBase}/hubs/tracking`, {
    accessTokenFactory: () => jwt
  })
  .withAutomaticReconnect()
  .build();

connection.on("pedidoActualizado", (orden) => {
  // mismo shape que GET /tracking: estado, pasos, tracking
});

await connection.start();
await connection.invoke("UnirseAPedido", codigoOrden);
```

3. Si el socket se cae, volver a `GET /api/orders/{codigo}/tracking`.

Solo el dueño del pedido puede unirse. Internos (supervisor/repartidor) no usan este hub.

Infra: redeploy del API. No hay variables nuevas ni `ALTER`.
