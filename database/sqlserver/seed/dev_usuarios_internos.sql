/*
    Seed de desarrollo para Backend Tienda y Negocio.
    No inserta compradores: el comprador vive en Oracle.
    El ADMIN ya viene en el script oficial de NextTechCustomDB.

    Crea admin@nexttech.com, supervisor@nexttech.com y repartidor@nexttech.com
    con la clave de prueba admin123 (hash ASP.NET Identity).
    Ejecutar en la SQL remota de Infra / NextTechCustomDB.
*/

USE [NextTechCustomDB];

IF NOT EXISTS (
    SELECT 1
    FROM dbo.UsuarioInterno
    WHERE Correo = N'correonexttechsolution933@gmail.com'
)
BEGIN
    THROW 50001, 'Primero ejecuta el script oficial para crear el ADMIN.', 1;
END;

DECLARE @HashPrueba NVARCHAR(500) =
    N'AQAAAAIAAYagAAAAEGhm9nLsfKqEUreP/Wg3LDkyQ92Nz0MLfjcQlDj9Xz11AQzOWrIny8aVu5P9khfQXw==';

UPDATE dbo.UsuarioInterno
SET Correo = N'supervisor@nexttech.com'
WHERE Correo = N'supervisor@nexttech.local';

UPDATE dbo.UsuarioInterno
SET Correo = N'repartidor@nexttech.com'
WHERE Correo = N'repartidor@nexttech.local';

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE Correo = N'admin@nexttech.com')
BEGIN
    INSERT dbo.UsuarioInterno
    (
        IdRol,
        Nombres,
        Apellidos,
        Correo,
        PasswordHash,
        Activo,
        DebeCambiarPassword
    )
    VALUES
    (
        1,
        N'Admin',
        N'Prueba',
        N'admin@nexttech.com',
        @HashPrueba,
        1,
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE Correo = N'supervisor@nexttech.com')
BEGIN
    INSERT dbo.UsuarioInterno
    (
        IdRol,
        Nombres,
        Apellidos,
        Correo,
        PasswordHash,
        Activo,
        DebeCambiarPassword
    )
    VALUES
    (
        2,
        N'Supervisor',
        N'Prueba',
        N'supervisor@nexttech.com',
        @HashPrueba,
        1,
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioInterno WHERE Correo = N'repartidor@nexttech.com')
BEGIN
    INSERT dbo.UsuarioInterno
    (
        IdRol,
        Nombres,
        Apellidos,
        Correo,
        PasswordHash,
        Activo,
        DebeCambiarPassword
    )
    VALUES
    (
        3,
        N'Repartidor',
        N'Prueba',
        N'repartidor@nexttech.com',
        @HashPrueba,
        1,
        0
    );
END;

UPDATE dbo.UsuarioInterno
SET PasswordHash = @HashPrueba,
    DebeCambiarPassword = 0,
    Activo = 1,
    IntentosFallidos = 0,
    BloqueadoHasta = NULL
WHERE Correo IN (N'admin@nexttech.com', N'supervisor@nexttech.com', N'repartidor@nexttech.com');
