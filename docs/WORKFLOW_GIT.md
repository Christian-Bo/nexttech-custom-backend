# Configuración GitHub recomendada

Después de subir el repositorio:

1. Crear la rama `develop` desde `main`.
2. Settings → General → Default branch → seleccionar `develop`.
3. Proteger `main`.
4. Proteger `develop`.

## Reglas recomendadas para main y develop

- Require a pull request before merging.
- Require at least 1 approval.
- Require status checks to pass.
- Seleccionar el check del workflow `Backend CI`.
- Require conversation resolution.
- Block force pushes.
- Block deletions.
- No permitir push directo.

## Merge

Usar preferentemente **Squash Merge**.

## Flujo

```text
feature/* -> develop -> main
```
