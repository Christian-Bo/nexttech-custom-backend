# Arquitectura Backend

## Decisión

Se utiliza **monolito modular**.

### Por qué

El proyecto requiere integración con una misma base SQL Server, Oracle externo, pagos, personalización y entregas. Microservicios agregarían despliegues, comunicación remota y transacciones distribuidas sin un beneficio proporcional para el equipo.

## Capas

### NextTech.Api
Entrada HTTP, autorización, middleware y configuración.

### NextTech.Application
Casos de uso y reglas de aplicación por módulo.

### NextTech.Domain
Modelo y reglas puras de dominio.

### NextTech.Infrastructure
SQL Server, Oracle y proveedores externos.

## Regla de dependencia

```text
Api -> Application
Api -> Infrastructure
Infrastructure -> Application
Infrastructure -> Domain
Application -> Domain
Domain -> nada
```

Domain no debe depender de Infrastructure ni Api.
