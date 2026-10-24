# NFC

El cliente elige el destino del chip (Spotify, YouTube, Facebook, etc.). El taller no escribe esa URL en la etiqueta: escribe un enlace corto de la orden. Al tocar el llavero, el API redirige al destino.

No hay `ALTER`. El destino vive en `PersonalizacionZona.ConfiguracionJson` como `nfcUrl`.

## Comprador: guardar el destino

`POST /api/personalizations` (JWT comprador) ahora exige `nfcUrl`:

```json
{
  "idVariante": 1,
  "nfcUrl": "https://open.spotify.com/playlist/37i9dQZF1DX4WYDNvnm2LO",
  "zonas": [
    {
      "idZona": 1,
      "configuracionJson": "{\"imagen\":\"...\"}",
      "imagenBase64": "...",
      "nombreArchivo": "lado-a.png",
      "tipoMime": "image/png"
    }
  ]
}
```

Si falta el esquema (`www.google.com`) o viene `http://`, el API lo guarda como `https://`. Si no es un enlace válido: **400** `La URL del NFC debe ser un enlace https válido.`

Si supera 2048 caracteres: **400** `La URL del NFC es demasiado larga. Usa un enlace más corto (máximo 2048 caracteres).`

`GET /api/personalizations/{id}` devuelve `nfcUrl`.

## Supervisor: grabar el chip

Lista (igual que ahora): `GET /api/supervisor/orders/in-production`

Detalle: `GET /api/supervisor/orders/{codigo}` ahora incluye:

- `nfcUrl` — lo que pegó el cliente (solo para confirmar)
- `nfcWriteUrl` — **esto** se escribe en la etiqueta, p. ej. `https://nexttech.136-113-48-173.sslip.io/n/ORD-656E6098`

También van en cada `items[]`.

Pantalla sugerida (Android, **Chrome o Edge**, HTTPS):

1. Abrir el pedido (Lado A y Lado B).
2. Botón **Grabar NFC**.
3. Texto: **Acerca la etiqueta a la parte de atrás del teléfono**
4. Web NFC:

```js
const writer = new NDEFReader();
await writer.write({
  records: [{ recordType: "url", data: nfcWriteUrl }]
});
```

5. Éxito: **Etiqueta lista. Al tocarla se abre el enlace del cliente.**
6. Si no detecta: **No se encontró la etiqueta. Acércala otra vez y mantenla quieta un segundo.**
7. Si no hay Web NFC (iPhone): **Este teléfono no puede grabar desde el navegador. Usa un Android con Chrome.**

La cámara no graba NFC.

## Lectura (el cliente toca el llavero)

`GET /n/{codigoOrden}` es anónimo. Responde **302** al `nfcUrl` del pedido. iPhone y Android leen el chip; no hace falta Chrome.

Infra: no agregar columnas. Solo redeploy cuando esto esté en `develop`.
