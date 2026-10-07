# Payments

Checkout con tarjeta via Recurrente. El backend nunca recibe ni guarda PAN, CVV ni fecha.

## Contrato para frontend

El widget de Recurrente **no pide llave pública**. Usa la URL que ya devuelve este API.

1. `POST /api/checkout` con JWT de comprador:

```json
{
  "idAreaEntrega": 1,
  "referenciaEntrega": "Edificio 4, aula 12",
  "metodoPago": "TARJETA"
}
```

2. Respuesta **200** (no crea la orden todavía):

```json
{
  "checkoutId": "ch_...",
  "checkoutUrl": "https://app.recurrente.com/checkout-session/ch_...",
  "url": "https://app.recurrente.com/checkout-session/ch_...",
  "total": 20.00,
  "moneda": "GTQ",
  "estado": "unpaid"
}
```

3. Widget:

```js
RecurrenteCheckout.load({
  url: checkoutUrl,
  onSuccess: async ({ checkoutId }) => {
    await fetch("/api/payments/recurrente/confirm", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify({ checkoutId })
    })
  }
})
```

`url` y `checkoutUrl` son la misma. Guardar `checkoutId` antes de montar el widget.

4. Confirm responde **201** con la orden (tracking, PDF, QR). Si el webhook ya la creó, el confirm es idempotente y devuelve la misma orden.

El backend precarga en Recurrente el correo, el nickname y el teléfono del comprador (Oracle). El widget no puede setear el país; Guatemala sale porque la moneda es GTQ.

Efectivo no cambia: `metodoPago: "EFECTIVO"` sigue creando la orden en el mismo `POST /api/checkout`.
