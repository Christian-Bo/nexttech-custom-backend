/*
    Seed de desarrollo para Backend Tienda y Negocio.
    No inserta compradores: el comprador vive en Oracle.
    El ADMIN ya viene en el script oficial de NextTechCustomDB.

    Ejecutar en SQL Server / NextTechCustomDB (la remota de Infra, no localhost)
    después del script oficial.
*/

USE [NextTechCustomDB];

DECLARE @HashAdmin NVARCHAR(500) =
(
    SELECT TOP (1) PasswordHash
    FROM dbo.UsuarioInterno
    WHERE Correo = N'correonexttechsolution933@gmail.com'
);

IF @HashAdmin IS NULL
BEGIN
    THROW 50001, 'Primero ejecuta el script oficial para crear el ADMIN.', 1;
END;

UPDATE dbo.UsuarioInterno
SET Correo = N'supervisor@nexttech.com'
WHERE Correo = N'supervisor@nexttech.local';

UPDATE dbo.UsuarioInterno
SET Correo = N'repartidor@nexttech.com'
WHERE Correo = N'repartidor@nexttech.local';

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
        @HashAdmin,
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
        @HashAdmin,
        1,
        0
    );
END;
