# Oracle central

Oracle es una base externa compartida.

Conexión actual:

```text
Host: www.server.daossystem.pro
Puerto: 5626
Servicio: XEPDB1
Usuario: TIENDA_APP
```

La contraseña NO se documenta aquí.

Cadena:

```text
User Id=TIENDA_APP;Password=<PASSWORD>;Data Source=www.server.daossystem.pro:5626/XEPDB1;
```

## Campos utilizados por NextTech

De `TIENDA_APP.USUARIO` se mapean inicialmente:

- ID_USUARIO
- CORREO
- TELEFONO
- FECHA_NACIMIENTO
- NICKNAME
- NOTIFICA_EMAIL
- NOTIFICA_WHATSAPP

No duplicar el comprador completo en SQL Server.

Oracle se trata como **solo lectura** desde este backend.
