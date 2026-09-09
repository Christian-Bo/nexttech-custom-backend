# Contribución

## Ramas

Usar:

- `feature/<descripcion>`
- `fix/<descripcion>`
- `docs/<descripcion>`
- `chore/<descripcion>`

Ejemplos:

```text
feature/oracle-auth
feature/cart
feature/order-checkout
fix/payment-validation
docs/database-rules
```

## Flujo

1. Crear rama desde `develop`.
2. Implementar cambios pequeños y enfocados.
3. Ejecutar build/tests.
4. Push de la rama.
5. Abrir PR hacia `develop`.
6. Esperar CI + revisión.
7. Hacer Squash Merge.
8. Solo `develop` se promueve a `main`.

No trabajar directamente sobre `main`.
