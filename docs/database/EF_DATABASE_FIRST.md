# EF Core y NextTechCustomDB

La base SQL Server ya existe y es la fuente oficial del modelo.

El proyecto incluye:

```text
Persistence/SqlServer/Entities/
Persistence/SqlServer/Configurations/
NextTechDbContext.cs
```

Cada tabla actual está mapeada a una entidad escalar. No se agregaron
navegaciones complejas ni lógica de negocio automáticamente para no imponer
decisiones innecesarias a los desarrolladores.

## Regla

Si cambia la DB:

1. acordar el cambio;
2. actualizar el script SQL;
3. ejecutar la actualización;
4. actualizar la entidad/configuración afectada;
5. agregar/actualizar pruebas;
6. PR.

No modificar la DB desde una migración improvisada.
