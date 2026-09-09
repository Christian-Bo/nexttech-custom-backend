# Oracle central

Oracle es un sistema externo/central compartido.

El backend debe consultar allí los compradores y sus datos centrales.

Datos confirmados relevantes del usuario central:

- `ID_USUARIO`
- `CORREO`
- `TELEFONO`
- `FECHA_NACIMIENTO`
- `NICKNAME`
- `PASSWORD_HASH`
- `TOKEN_QR_HASH`
- `NOTIFICA_EMAIL`
- `NOTIFICA_WHATSAPP`
- controles centrales de acceso/bloqueo

## Mapeo hacia SQL Server

```text
Oracle.USUARIO.ID_USUARIO -> IdCompradorExterno
Oracle.USUARIO.NICKNAME    -> NicknameCompradorAplicado (snapshot en Orden)
Oracle.USUARIO.CORREO      -> CorreoCompradorAplicado
Oracle.USUARIO.TELEFONO    -> TelefonoCompradorAplicado
```

No crear FK entre bases y no duplicar la tabla de compradores en NextTechCustomDB.

No colocar credenciales Oracle en este repositorio.
