/*
    NextTechCustomDB - VERSION ACTUALIZADA PARA DBEAVER + SQL SERVER
    ----------------------------------------------------------------
    CAMBIO REALIZADO:
      dbo.Orden.NombreCompradorAplicado
      -> dbo.Orden.NicknameCompradorAplicado NVARCHAR(50)

    No se realizaron cambios grandes en la estructura.
    Se mantiene el mismo modelo, tablas, relaciones, constraints,
    indices, catalogos y seeds definidos anteriormente.

    IMPORTANTE:
    - Para una instalacion nueva, Orden se crea directamente con
      NicknameCompradorAplicado.
    - Para una base ya creada con la version anterior, este script
      incluye una migracion segura que renombra la columna antigua.
    - El comprador sigue siendo externo y proviene de Oracle.
    - IdCompradorExterno se mantiene como BIGINT.
    - Sin triggers.
    - Sin stored procedures ni funciones de negocio.
*/

USE master;

IF DB_ID(N'NextTechCustomDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [NextTechCustomDB];');
END;

USE [NextTechCustomDB];

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ============================================================
       1. CATALOGOS FIJOS
       ============================================================ */

    IF OBJECT_ID(N'dbo.Rol', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Rol
        (
            IdRol INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_Rol PRIMARY KEY (IdRol),
            CONSTRAINT UQ_Rol_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.EstadoOrden', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadoOrden
        (
            IdEstadoOrden INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_EstadoOrden PRIMARY KEY (IdEstadoOrden),
            CONSTRAINT UQ_EstadoOrden_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.EstadoCarrito', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadoCarrito
        (
            IdEstadoCarrito INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_EstadoCarrito PRIMARY KEY (IdEstadoCarrito),
            CONSTRAINT UQ_EstadoCarrito_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.MetodoPago', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MetodoPago
        (
            IdMetodoPago INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_MetodoPago PRIMARY KEY (IdMetodoPago),
            CONSTRAINT UQ_MetodoPago_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.EstadoPago', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadoPago
        (
            IdEstadoPago INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_EstadoPago PRIMARY KEY (IdEstadoPago),
            CONSTRAINT UQ_EstadoPago_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.ResultadoEntrega', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ResultadoEntrega
        (
            IdResultadoEntrega INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_ResultadoEntrega PRIMARY KEY (IdResultadoEntrega),
            CONSTRAINT UQ_ResultadoEntrega_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.TipoActor', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TipoActor
        (
            IdTipoActor INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_TipoActor PRIMARY KEY (IdTipoActor),
            CONSTRAINT UQ_TipoActor_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.TipoNotificacion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TipoNotificacion
        (
            IdTipoNotificacion INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_TipoNotificacion PRIMARY KEY (IdTipoNotificacion),
            CONSTRAINT UQ_TipoNotificacion_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.CanalNotificacion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CanalNotificacion
        (
            IdCanalNotificacion INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_CanalNotificacion PRIMARY KEY (IdCanalNotificacion),
            CONSTRAINT UQ_CanalNotificacion_Codigo UNIQUE (Codigo)
        );
    END;

    IF OBJECT_ID(N'dbo.EstadoNotificacion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EstadoNotificacion
        (
            IdEstadoNotificacion INT IDENTITY(1,1) NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_EstadoNotificacion PRIMARY KEY (IdEstadoNotificacion),
            CONSTRAINT UQ_EstadoNotificacion_Codigo UNIQUE (Codigo)
        );
    END;

    /* ============================================================
       2. ARCHIVOS Y USUARIOS INTERNOS
       ============================================================ */

    IF OBJECT_ID(N'dbo.Archivo', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Archivo
        (
            IdArchivo INT IDENTITY(1,1) NOT NULL,
            NombreOriginal NVARCHAR(255) NOT NULL,
            TipoMime NVARCHAR(100) NOT NULL,
            Extension NVARCHAR(20) NOT NULL,
            Datos VARBINARY(MAX) NOT NULL,
            TamanoBytes BIGINT NOT NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Archivo_FechaCreacion DEFAULT SYSDATETIME(),
            CONSTRAINT PK_Archivo PRIMARY KEY (IdArchivo),
            CONSTRAINT CK_Archivo_TamanoBytes CHECK (TamanoBytes > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.UsuarioInterno', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UsuarioInterno
        (
            IdUsuarioInterno INT IDENTITY(1,1) NOT NULL,
            IdRol INT NOT NULL,
            Nombres NVARCHAR(100) NOT NULL,
            Apellidos NVARCHAR(100) NOT NULL,
            Correo NVARCHAR(200) NOT NULL,
            PasswordHash NVARCHAR(500) NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_UsuarioInterno_Activo DEFAULT (1),
            DebeCambiarPassword BIT NOT NULL
                CONSTRAINT DF_UsuarioInterno_DebeCambiarPassword DEFAULT (0),
            IntentosFallidos INT NOT NULL
                CONSTRAINT DF_UsuarioInterno_IntentosFallidos DEFAULT (0),
            BloqueadoHasta DATETIME2(0) NULL,
            UltimoAcceso DATETIME2(0) NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_UsuarioInterno_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_UsuarioInterno PRIMARY KEY (IdUsuarioInterno),
            CONSTRAINT UQ_UsuarioInterno_Correo UNIQUE (Correo),
            CONSTRAINT FK_UsuarioInterno_Rol
                FOREIGN KEY (IdRol) REFERENCES dbo.Rol(IdRol) ON DELETE NO ACTION,
            CONSTRAINT CK_UsuarioInterno_IntentosFallidos
                CHECK (IntentosFallidos >= 0),
            CONSTRAINT CK_UsuarioInterno_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    /* ============================================================
       3. CATALOGO DE PRODUCTOS
       ============================================================ */

    IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Categoria
        (
            IdCategoria INT IDENTITY(1,1) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(500) NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_Categoria_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Categoria_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_Categoria PRIMARY KEY (IdCategoria),
            CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_Categoria_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.Producto', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Producto
        (
            IdProducto INT IDENTITY(1,1) NOT NULL,
            IdCategoria INT NOT NULL,
            CodigoProducto NVARCHAR(30) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(1000) NULL,
            PrecioBase DECIMAL(10,2) NOT NULL,
            PermitePersonalizacion BIT NOT NULL
                CONSTRAINT DF_Producto_PermitePersonalizacion DEFAULT (1),
            Activo BIT NOT NULL
                CONSTRAINT DF_Producto_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Producto_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_Producto PRIMARY KEY (IdProducto),
            CONSTRAINT UQ_Producto_CodigoProducto UNIQUE (CodigoProducto),
            CONSTRAINT FK_Producto_Categoria
                FOREIGN KEY (IdCategoria) REFERENCES dbo.Categoria(IdCategoria) ON DELETE NO ACTION,
            CONSTRAINT CK_Producto_PrecioBase CHECK (PrecioBase > 0),
            CONSTRAINT CK_Producto_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.VarianteProducto', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VarianteProducto
        (
            IdVariante INT IDENTITY(1,1) NOT NULL,
            IdProducto INT NOT NULL,
            CodigoVariante NVARCHAR(30) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(500) NULL,
            PrecioAdicional DECIMAL(10,2) NOT NULL
                CONSTRAINT DF_VarianteProducto_PrecioAdicional DEFAULT (0),
            Activo BIT NOT NULL
                CONSTRAINT DF_VarianteProducto_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_VarianteProducto_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_VarianteProducto PRIMARY KEY (IdVariante),
            CONSTRAINT UQ_VarianteProducto_CodigoVariante UNIQUE (CodigoVariante),
            CONSTRAINT UQ_VarianteProducto_Producto_Nombre UNIQUE (IdProducto, Nombre),
            CONSTRAINT FK_VarianteProducto_Producto
                FOREIGN KEY (IdProducto) REFERENCES dbo.Producto(IdProducto) ON DELETE NO ACTION,
            CONSTRAINT CK_VarianteProducto_PrecioAdicional CHECK (PrecioAdicional >= 0),
            CONSTRAINT CK_VarianteProducto_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.Atributo', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Atributo
        (
            IdAtributo INT IDENTITY(1,1) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_Atributo_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Atributo_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_Atributo PRIMARY KEY (IdAtributo),
            CONSTRAINT UQ_Atributo_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_Atributo_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.ValorAtributo', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ValorAtributo
        (
            IdValorAtributo INT IDENTITY(1,1) NOT NULL,
            IdAtributo INT NOT NULL,
            Valor NVARCHAR(100) NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_ValorAtributo_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ValorAtributo_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_ValorAtributo PRIMARY KEY (IdValorAtributo),
            CONSTRAINT UQ_ValorAtributo_Atributo_Valor UNIQUE (IdAtributo, Valor),
            CONSTRAINT UQ_ValorAtributo_Atributo_IdValor UNIQUE (IdAtributo, IdValorAtributo),
            CONSTRAINT FK_ValorAtributo_Atributo
                FOREIGN KEY (IdAtributo) REFERENCES dbo.Atributo(IdAtributo) ON DELETE NO ACTION,
            CONSTRAINT CK_ValorAtributo_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.ProductoAtributo', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductoAtributo
        (
            IdProductoAtributo INT IDENTITY(1,1) NOT NULL,
            IdProducto INT NOT NULL,
            IdAtributo INT NOT NULL,
            EsObligatorio BIT NOT NULL
                CONSTRAINT DF_ProductoAtributo_EsObligatorio DEFAULT (1),
            OrdenVisual INT NOT NULL,

            CONSTRAINT PK_ProductoAtributo PRIMARY KEY (IdProductoAtributo),
            CONSTRAINT UQ_ProductoAtributo_Producto_Atributo UNIQUE (IdProducto, IdAtributo),
            CONSTRAINT UQ_ProductoAtributo_Producto_Orden UNIQUE (IdProducto, OrdenVisual),
            CONSTRAINT FK_ProductoAtributo_Producto
                FOREIGN KEY (IdProducto) REFERENCES dbo.Producto(IdProducto) ON DELETE NO ACTION,
            CONSTRAINT FK_ProductoAtributo_Atributo
                FOREIGN KEY (IdAtributo) REFERENCES dbo.Atributo(IdAtributo) ON DELETE NO ACTION,
            CONSTRAINT CK_ProductoAtributo_OrdenVisual CHECK (OrdenVisual > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.VarianteAtributo', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VarianteAtributo
        (
            IdVarianteAtributo INT IDENTITY(1,1) NOT NULL,
            IdVariante INT NOT NULL,
            IdAtributo INT NOT NULL,
            IdValorAtributo INT NOT NULL,

            CONSTRAINT PK_VarianteAtributo PRIMARY KEY (IdVarianteAtributo),
            CONSTRAINT UQ_VarianteAtributo_Variante_Atributo UNIQUE (IdVariante, IdAtributo),
            CONSTRAINT UQ_VarianteAtributo_Variante_Valor UNIQUE (IdVariante, IdValorAtributo),
            CONSTRAINT FK_VarianteAtributo_Variante
                FOREIGN KEY (IdVariante) REFERENCES dbo.VarianteProducto(IdVariante) ON DELETE NO ACTION,
            CONSTRAINT FK_VarianteAtributo_Atributo_Valor
                FOREIGN KEY (IdAtributo, IdValorAtributo)
                REFERENCES dbo.ValorAtributo(IdAtributo, IdValorAtributo)
                ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.ProductoImagen', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductoImagen
        (
            IdProductoImagen INT IDENTITY(1,1) NOT NULL,
            IdProducto INT NOT NULL,
            IdArchivo INT NOT NULL,
            EsPrincipal BIT NOT NULL
                CONSTRAINT DF_ProductoImagen_EsPrincipal DEFAULT (0),
            OrdenVisual INT NOT NULL
                CONSTRAINT DF_ProductoImagen_OrdenVisual DEFAULT (1),

            CONSTRAINT PK_ProductoImagen PRIMARY KEY (IdProductoImagen),
            CONSTRAINT UQ_ProductoImagen_Producto_Orden UNIQUE (IdProducto, OrdenVisual),
            CONSTRAINT UQ_ProductoImagen_Producto_Archivo UNIQUE (IdProducto, IdArchivo),
            CONSTRAINT FK_ProductoImagen_Producto
                FOREIGN KEY (IdProducto) REFERENCES dbo.Producto(IdProducto) ON DELETE NO ACTION,
            CONSTRAINT FK_ProductoImagen_Archivo
                FOREIGN KEY (IdArchivo) REFERENCES dbo.Archivo(IdArchivo) ON DELETE NO ACTION,
            CONSTRAINT CK_ProductoImagen_OrdenVisual CHECK (OrdenVisual > 0)
        );
    END;

    IF OBJECT_ID(N'dbo.ZonaPersonalizacion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ZonaPersonalizacion
        (
            IdZona INT IDENTITY(1,1) NOT NULL,
            IdProducto INT NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            EsObligatoria BIT NOT NULL
                CONSTRAINT DF_ZonaPersonalizacion_EsObligatoria DEFAULT (1),
            OrdenVisual INT NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_ZonaPersonalizacion_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ZonaPersonalizacion_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_ZonaPersonalizacion PRIMARY KEY (IdZona),
            CONSTRAINT UQ_ZonaPersonalizacion_Producto_Nombre UNIQUE (IdProducto, Nombre),
            CONSTRAINT UQ_ZonaPersonalizacion_Producto_Orden UNIQUE (IdProducto, OrdenVisual),
            CONSTRAINT FK_ZonaPersonalizacion_Producto
                FOREIGN KEY (IdProducto) REFERENCES dbo.Producto(IdProducto) ON DELETE NO ACTION,
            CONSTRAINT CK_ZonaPersonalizacion_OrdenVisual CHECK (OrdenVisual > 0),
            CONSTRAINT CK_ZonaPersonalizacion_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.PlantillaVarianteZona', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlantillaVarianteZona
        (
            IdPlantilla INT IDENTITY(1,1) NOT NULL,
            IdVariante INT NOT NULL,
            IdZona INT NOT NULL,
            Forma NVARCHAR(50) NOT NULL,
            AnchoLienzo INT NOT NULL,
            AltoLienzo INT NOT NULL,
            IdArchivoMascara INT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_PlantillaVarianteZona_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_PlantillaVarianteZona_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_PlantillaVarianteZona PRIMARY KEY (IdPlantilla),
            CONSTRAINT UQ_PlantillaVarianteZona_Variante_Zona UNIQUE (IdVariante, IdZona),
            CONSTRAINT FK_PlantillaVarianteZona_Variante
                FOREIGN KEY (IdVariante) REFERENCES dbo.VarianteProducto(IdVariante) ON DELETE NO ACTION,
            CONSTRAINT FK_PlantillaVarianteZona_Zona
                FOREIGN KEY (IdZona) REFERENCES dbo.ZonaPersonalizacion(IdZona) ON DELETE NO ACTION,
            CONSTRAINT FK_PlantillaVarianteZona_ArchivoMascara
                FOREIGN KEY (IdArchivoMascara) REFERENCES dbo.Archivo(IdArchivo) ON DELETE NO ACTION,
            CONSTRAINT CK_PlantillaVarianteZona_Ancho CHECK (AnchoLienzo > 0),
            CONSTRAINT CK_PlantillaVarianteZona_Alto CHECK (AltoLienzo > 0),
            CONSTRAINT CK_PlantillaVarianteZona_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.AreaEntrega', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AreaEntrega
        (
            IdAreaEntrega INT IDENTITY(1,1) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(500) NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_AreaEntrega_Activo DEFAULT (1),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_AreaEntrega_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            FechaDesactivacion DATETIME2(0) NULL,

            CONSTRAINT PK_AreaEntrega PRIMARY KEY (IdAreaEntrega),
            CONSTRAINT UQ_AreaEntrega_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_AreaEntrega_Activo_FechaDesactivacion
                CHECK (
                    (Activo = 1 AND FechaDesactivacion IS NULL)
                    OR
                    (Activo = 0 AND FechaDesactivacion IS NOT NULL)
                )
        );
    END;

    /* ============================================================
       4. PERSONALIZACION
       ============================================================ */

    IF OBJECT_ID(N'dbo.Personalizacion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Personalizacion
        (
            IdPersonalizacion INT IDENTITY(1,1) NOT NULL,
            IdVariante INT NOT NULL,
            Bloqueada BIT NOT NULL
                CONSTRAINT DF_Personalizacion_Bloqueada DEFAULT (0),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Personalizacion_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,

            CONSTRAINT PK_Personalizacion PRIMARY KEY (IdPersonalizacion),
            CONSTRAINT UQ_Personalizacion_Id_Variante UNIQUE (IdPersonalizacion, IdVariante),
            CONSTRAINT FK_Personalizacion_Variante
                FOREIGN KEY (IdVariante) REFERENCES dbo.VarianteProducto(IdVariante) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.PersonalizacionZona', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PersonalizacionZona
        (
            IdPersonalizacionZona INT IDENTITY(1,1) NOT NULL,
            IdPersonalizacion INT NOT NULL,
            IdZona INT NOT NULL,
            IdArchivoImagenFinal INT NOT NULL,
            ConfiguracionJson NVARCHAR(MAX) NOT NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_PersonalizacionZona_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,

            CONSTRAINT PK_PersonalizacionZona PRIMARY KEY (IdPersonalizacionZona),
            CONSTRAINT UQ_PersonalizacionZona_Personalizacion_Zona UNIQUE (IdPersonalizacion, IdZona),
            CONSTRAINT FK_PersonalizacionZona_Personalizacion
                FOREIGN KEY (IdPersonalizacion) REFERENCES dbo.Personalizacion(IdPersonalizacion) ON DELETE NO ACTION,
            CONSTRAINT FK_PersonalizacionZona_Zona
                FOREIGN KEY (IdZona) REFERENCES dbo.ZonaPersonalizacion(IdZona) ON DELETE NO ACTION,
            CONSTRAINT FK_PersonalizacionZona_ArchivoFinal
                FOREIGN KEY (IdArchivoImagenFinal) REFERENCES dbo.Archivo(IdArchivo) ON DELETE NO ACTION,
            CONSTRAINT CK_PersonalizacionZona_ConfiguracionJson
                CHECK (ISJSON(ConfiguracionJson) = 1)
        );
    END;

    /* ============================================================
       5. CARRITO
       ============================================================ */

    IF OBJECT_ID(N'dbo.Carrito', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Carrito
        (
            IdCarrito INT IDENTITY(1,1) NOT NULL,
            IdCompradorExterno BIGINT NOT NULL,
            IdEstadoCarrito INT NOT NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Carrito_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,
            UltimaActividad DATETIME2(0) NOT NULL
                CONSTRAINT DF_Carrito_UltimaActividad DEFAULT SYSDATETIME(),

            CONSTRAINT PK_Carrito PRIMARY KEY (IdCarrito),
            CONSTRAINT FK_Carrito_EstadoCarrito
                FOREIGN KEY (IdEstadoCarrito) REFERENCES dbo.EstadoCarrito(IdEstadoCarrito) ON DELETE NO ACTION
        );
    END;

    IF OBJECT_ID(N'dbo.DetalleCarrito', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DetalleCarrito
        (
            IdDetalleCarrito INT IDENTITY(1,1) NOT NULL,
            IdCarrito INT NOT NULL,
            IdVariante INT NOT NULL,
            Cantidad INT NOT NULL,
            IdPersonalizacion INT NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_DetalleCarrito_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,

            CONSTRAINT PK_DetalleCarrito PRIMARY KEY (IdDetalleCarrito),
            CONSTRAINT FK_DetalleCarrito_Carrito
                FOREIGN KEY (IdCarrito) REFERENCES dbo.Carrito(IdCarrito) ON DELETE NO ACTION,
            CONSTRAINT FK_DetalleCarrito_Variante
                FOREIGN KEY (IdVariante) REFERENCES dbo.VarianteProducto(IdVariante) ON DELETE NO ACTION,
            CONSTRAINT FK_DetalleCarrito_Personalizacion_Variante
                FOREIGN KEY (IdPersonalizacion, IdVariante)
                REFERENCES dbo.Personalizacion(IdPersonalizacion, IdVariante)
                ON DELETE NO ACTION,
            CONSTRAINT CK_DetalleCarrito_Cantidad CHECK (Cantidad > 0)
        );
    END;

    /* ============================================================
       6. ORDENES
       ============================================================ */

    IF OBJECT_ID(N'dbo.Orden', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Orden
        (
            IdOrden INT IDENTITY(1,1) NOT NULL,
            CodigoOrden NVARCHAR(30) NOT NULL,
            IdCompradorExterno BIGINT NOT NULL,

            -- CAMBIO: antes NombreCompradorAplicado NVARCHAR(200)
            NicknameCompradorAplicado NVARCHAR(50) NOT NULL,

            CorreoCompradorAplicado NVARCHAR(200) NOT NULL,
            TelefonoCompradorAplicado NVARCHAR(30) NULL,
            IdCarritoOrigen INT NULL,
            IdAreaEntrega INT NOT NULL,
            NombreAreaAplicado NVARCHAR(150) NOT NULL,
            ReferenciaEntrega NVARCHAR(300) NOT NULL,
            IdEstadoOrdenActual INT NOT NULL,
            IdRepartidorAsignado INT NULL,
            Total DECIMAL(10,2) NOT NULL,
            IdArchivoConstancia INT NULL,
            QrUtilizado BIT NOT NULL
                CONSTRAINT DF_Orden_QrUtilizado DEFAULT (0),
            FechaUsoQr DATETIME2(0) NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Orden_FechaCreacion DEFAULT SYSDATETIME(),
            FechaActualizacion DATETIME2(0) NULL,

            CONSTRAINT PK_Orden PRIMARY KEY (IdOrden),
            CONSTRAINT UQ_Orden_CodigoOrden UNIQUE (CodigoOrden),
            CONSTRAINT FK_Orden_CarritoOrigen
                FOREIGN KEY (IdCarritoOrigen) REFERENCES dbo.Carrito(IdCarrito) ON DELETE NO ACTION,
            CONSTRAINT FK_Orden_AreaEntrega
                FOREIGN KEY (IdAreaEntrega) REFERENCES dbo.AreaEntrega(IdAreaEntrega) ON DELETE NO ACTION,
            CONSTRAINT FK_Orden_EstadoOrden
                FOREIGN KEY (IdEstadoOrdenActual) REFERENCES dbo.EstadoOrden(IdEstadoOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_Orden_Repartidor
                FOREIGN KEY (IdRepartidorAsignado) REFERENCES dbo.UsuarioInterno(IdUsuarioInterno) ON DELETE NO ACTION,
            CONSTRAINT FK_Orden_ArchivoConstancia
                FOREIGN KEY (IdArchivoConstancia) REFERENCES dbo.Archivo(IdArchivo) ON DELETE NO ACTION,
            CONSTRAINT CK_Orden_Total CHECK (Total > 0),
            CONSTRAINT CK_Orden_Qr_Fecha
                CHECK (
                    (QrUtilizado = 0 AND FechaUsoQr IS NULL)
                    OR
                    (QrUtilizado = 1 AND FechaUsoQr IS NOT NULL)
                )
        );
    END;

    /*
       MIGRACION SEGURA PARA BASE YA EXISTENTE
       Si la version anterior tenia NombreCompradorAplicado,
       se renombra sin borrar la tabla ni sus datos.
    */
    IF COL_LENGTH(N'dbo.Orden', N'NombreCompradorAplicado') IS NOT NULL
       AND COL_LENGTH(N'dbo.Orden', N'NicknameCompradorAplicado') IS NULL
    BEGIN
        EXEC sp_rename
            N'dbo.Orden.NombreCompradorAplicado',
            N'NicknameCompradorAplicado',
            N'COLUMN';
    END;

    IF COL_LENGTH(N'dbo.Orden', N'NicknameCompradorAplicado') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Orden
        ALTER COLUMN NicknameCompradorAplicado NVARCHAR(50) NOT NULL;
    END;

    IF OBJECT_ID(N'dbo.DetalleOrden', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.DetalleOrden
        (
            IdDetalleOrden INT IDENTITY(1,1) NOT NULL,
            IdOrden INT NOT NULL,
            IdVariante INT NOT NULL,
            IdPersonalizacion INT NULL,
            Cantidad INT NOT NULL,
            NombreProductoAplicado NVARCHAR(150) NOT NULL,
            NombreVarianteAplicada NVARCHAR(150) NOT NULL,
            AtributosAplicadosJson NVARCHAR(MAX) NOT NULL,
            PrecioBaseAplicado DECIMAL(10,2) NOT NULL,
            PrecioVarianteAplicado DECIMAL(10,2) NOT NULL,
            PrecioUnitario DECIMAL(10,2) NOT NULL,
            Subtotal DECIMAL(10,2) NOT NULL,

            CONSTRAINT PK_DetalleOrden PRIMARY KEY (IdDetalleOrden),
            CONSTRAINT FK_DetalleOrden_Orden
                FOREIGN KEY (IdOrden) REFERENCES dbo.Orden(IdOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_DetalleOrden_Variante
                FOREIGN KEY (IdVariante) REFERENCES dbo.VarianteProducto(IdVariante) ON DELETE NO ACTION,
            CONSTRAINT FK_DetalleOrden_Personalizacion_Variante
                FOREIGN KEY (IdPersonalizacion, IdVariante)
                REFERENCES dbo.Personalizacion(IdPersonalizacion, IdVariante)
                ON DELETE NO ACTION,
            CONSTRAINT CK_DetalleOrden_Cantidad CHECK (Cantidad > 0),
            CONSTRAINT CK_DetalleOrden_AtributosJson CHECK (ISJSON(AtributosAplicadosJson) = 1),
            CONSTRAINT CK_DetalleOrden_PrecioBase CHECK (PrecioBaseAplicado > 0),
            CONSTRAINT CK_DetalleOrden_PrecioVariante CHECK (PrecioVarianteAplicado >= 0),
            CONSTRAINT CK_DetalleOrden_PrecioUnitario
                CHECK (PrecioUnitario = PrecioBaseAplicado + PrecioVarianteAplicado),
            CONSTRAINT CK_DetalleOrden_Subtotal
                CHECK (Subtotal = PrecioUnitario * Cantidad)
        );
    END;

    IF OBJECT_ID(N'dbo.Pago', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Pago
        (
            IdPago INT IDENTITY(1,1) NOT NULL,
            IdOrden INT NOT NULL,
            IdMetodoPago INT NOT NULL,
            IdEstadoPago INT NOT NULL,
            Monto DECIMAL(10,2) NOT NULL,
            ReferenciaTransaccion NVARCHAR(150) NULL,
            MarcaTarjeta NVARCHAR(30) NULL,
            Ultimos4 NVARCHAR(4) NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Pago_FechaCreacion DEFAULT SYSDATETIME(),
            FechaPago DATETIME2(0) NULL,

            CONSTRAINT PK_Pago PRIMARY KEY (IdPago),
            CONSTRAINT UQ_Pago_IdOrden UNIQUE (IdOrden),
            CONSTRAINT FK_Pago_Orden
                FOREIGN KEY (IdOrden) REFERENCES dbo.Orden(IdOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_Pago_MetodoPago
                FOREIGN KEY (IdMetodoPago) REFERENCES dbo.MetodoPago(IdMetodoPago) ON DELETE NO ACTION,
            CONSTRAINT FK_Pago_EstadoPago
                FOREIGN KEY (IdEstadoPago) REFERENCES dbo.EstadoPago(IdEstadoPago) ON DELETE NO ACTION,
            CONSTRAINT CK_Pago_Monto CHECK (Monto > 0),
            CONSTRAINT CK_Pago_FechaPago
                CHECK (FechaPago IS NULL OR FechaPago >= FechaCreacion),
            CONSTRAINT CK_Pago_Ultimos4
                CHECK (Ultimos4 IS NULL OR LEN(Ultimos4) = 4)
        );
    END;

    IF OBJECT_ID(N'dbo.HistorialEstadoOrden', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HistorialEstadoOrden
        (
            IdHistorialEstado INT IDENTITY(1,1) NOT NULL,
            IdOrden INT NOT NULL,
            IdEstadoOrden INT NOT NULL,
            IdTipoActor INT NOT NULL,
            IdUsuarioInterno INT NULL,
            IdCompradorExterno BIGINT NULL,
            FechaHora DATETIME2(0) NOT NULL
                CONSTRAINT DF_HistorialEstadoOrden_FechaHora DEFAULT SYSDATETIME(),
            Observacion NVARCHAR(500) NULL,

            CONSTRAINT PK_HistorialEstadoOrden PRIMARY KEY (IdHistorialEstado),
            CONSTRAINT FK_HistorialEstadoOrden_Orden
                FOREIGN KEY (IdOrden) REFERENCES dbo.Orden(IdOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_HistorialEstadoOrden_EstadoOrden
                FOREIGN KEY (IdEstadoOrden) REFERENCES dbo.EstadoOrden(IdEstadoOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_HistorialEstadoOrden_TipoActor
                FOREIGN KEY (IdTipoActor) REFERENCES dbo.TipoActor(IdTipoActor) ON DELETE NO ACTION,
            CONSTRAINT FK_HistorialEstadoOrden_UsuarioInterno
                FOREIGN KEY (IdUsuarioInterno) REFERENCES dbo.UsuarioInterno(IdUsuarioInterno) ON DELETE NO ACTION,
            CONSTRAINT CK_HistorialEstadoOrden_Actor
                CHECK (
                    (IdTipoActor = 1 AND IdUsuarioInterno IS NULL AND IdCompradorExterno IS NULL)
                    OR
                    (IdTipoActor = 2 AND IdUsuarioInterno IS NOT NULL AND IdCompradorExterno IS NULL)
                    OR
                    (IdTipoActor = 3 AND IdUsuarioInterno IS NULL AND IdCompradorExterno IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.IntentoEntrega', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IntentoEntrega
        (
            IdIntentoEntrega INT IDENTITY(1,1) NOT NULL,
            IdOrden INT NOT NULL,
            IdRepartidor INT NOT NULL,
            FechaHoraInicio DATETIME2(0) NOT NULL
                CONSTRAINT DF_IntentoEntrega_FechaHoraInicio DEFAULT SYSDATETIME(),
            FechaHoraFin DATETIME2(0) NULL,
            IdResultadoEntrega INT NULL,
            Observacion NVARCHAR(500) NULL,
            IdArchivoFotoEntrega INT NULL,

            CONSTRAINT PK_IntentoEntrega PRIMARY KEY (IdIntentoEntrega),
            CONSTRAINT FK_IntentoEntrega_Orden
                FOREIGN KEY (IdOrden) REFERENCES dbo.Orden(IdOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_IntentoEntrega_Repartidor
                FOREIGN KEY (IdRepartidor) REFERENCES dbo.UsuarioInterno(IdUsuarioInterno) ON DELETE NO ACTION,
            CONSTRAINT FK_IntentoEntrega_Resultado
                FOREIGN KEY (IdResultadoEntrega) REFERENCES dbo.ResultadoEntrega(IdResultadoEntrega) ON DELETE NO ACTION,
            CONSTRAINT FK_IntentoEntrega_ArchivoFoto
                FOREIGN KEY (IdArchivoFotoEntrega) REFERENCES dbo.Archivo(IdArchivo) ON DELETE NO ACTION,
            CONSTRAINT CK_IntentoEntrega_Fechas
                CHECK (FechaHoraFin IS NULL OR FechaHoraFin >= FechaHoraInicio)
        );
    END;

    IF OBJECT_ID(N'dbo.Notificacion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Notificacion
        (
            IdNotificacion INT IDENTITY(1,1) NOT NULL,
            IdOrden INT NOT NULL,
            IdTipoNotificacion INT NOT NULL,
            IdCanalNotificacion INT NOT NULL,
            IdEstadoNotificacion INT NOT NULL,
            Destino NVARCHAR(200) NOT NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_Notificacion_FechaCreacion DEFAULT SYSDATETIME(),
            FechaEnvio DATETIME2(0) NULL,
            Intentos INT NOT NULL
                CONSTRAINT DF_Notificacion_Intentos DEFAULT (0),
            MensajeError NVARCHAR(1000) NULL,

            CONSTRAINT PK_Notificacion PRIMARY KEY (IdNotificacion),
            CONSTRAINT UQ_Notificacion_Orden_Tipo_Canal
                UNIQUE (IdOrden, IdTipoNotificacion, IdCanalNotificacion),
            CONSTRAINT FK_Notificacion_Orden
                FOREIGN KEY (IdOrden) REFERENCES dbo.Orden(IdOrden) ON DELETE NO ACTION,
            CONSTRAINT FK_Notificacion_Tipo
                FOREIGN KEY (IdTipoNotificacion) REFERENCES dbo.TipoNotificacion(IdTipoNotificacion) ON DELETE NO ACTION,
            CONSTRAINT FK_Notificacion_Canal
                FOREIGN KEY (IdCanalNotificacion) REFERENCES dbo.CanalNotificacion(IdCanalNotificacion) ON DELETE NO ACTION,
            CONSTRAINT FK_Notificacion_Estado
                FOREIGN KEY (IdEstadoNotificacion) REFERENCES dbo.EstadoNotificacion(IdEstadoNotificacion) ON DELETE NO ACTION,
            CONSTRAINT CK_Notificacion_Intentos CHECK (Intentos >= 0)
        );
    END;

    IF OBJECT_ID(N'dbo.BitacoraAuditoria', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BitacoraAuditoria
        (
            IdBitacora INT IDENTITY(1,1) NOT NULL,
            IdTipoActor INT NOT NULL,
            IdUsuarioInterno INT NULL,
            IdCompradorExterno BIGINT NULL,
            Accion NVARCHAR(100) NOT NULL,
            Entidad NVARCHAR(100) NOT NULL,
            IdEntidad INT NULL,
            Resultado NVARCHAR(50) NOT NULL,
            DireccionIp NVARCHAR(45) NULL,
            Detalle NVARCHAR(1000) NULL,
            FechaHora DATETIME2(0) NOT NULL
                CONSTRAINT DF_BitacoraAuditoria_FechaHora DEFAULT SYSDATETIME(),

            CONSTRAINT PK_BitacoraAuditoria PRIMARY KEY (IdBitacora),
            CONSTRAINT FK_BitacoraAuditoria_TipoActor
                FOREIGN KEY (IdTipoActor) REFERENCES dbo.TipoActor(IdTipoActor) ON DELETE NO ACTION,
            CONSTRAINT FK_BitacoraAuditoria_UsuarioInterno
                FOREIGN KEY (IdUsuarioInterno) REFERENCES dbo.UsuarioInterno(IdUsuarioInterno) ON DELETE NO ACTION,
            CONSTRAINT CK_BitacoraAuditoria_Actor
                CHECK (
                    (IdTipoActor = 1 AND IdUsuarioInterno IS NULL AND IdCompradorExterno IS NULL)
                    OR
                    (IdTipoActor = 2 AND IdUsuarioInterno IS NOT NULL AND IdCompradorExterno IS NULL)
                    OR
                    (IdTipoActor = 3 AND IdUsuarioInterno IS NULL AND IdCompradorExterno IS NOT NULL)
                )
        );
    END;

    /* ============================================================
       7. INDICES DE INTEGRIDAD Y RENDIMIENTO
       ============================================================ */

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ProductoImagen_Principal' AND object_id = OBJECT_ID(N'dbo.ProductoImagen'))
        CREATE UNIQUE INDEX UX_ProductoImagen_Principal
        ON dbo.ProductoImagen(IdProducto)
        WHERE EsPrincipal = 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Carrito_Comprador_Activo' AND object_id = OBJECT_ID(N'dbo.Carrito'))
        CREATE UNIQUE INDEX UX_Carrito_Comprador_Activo
        ON dbo.Carrito(IdCompradorExterno)
        WHERE IdEstadoCarrito = 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DetalleCarrito_Personalizacion' AND object_id = OBJECT_ID(N'dbo.DetalleCarrito'))
        CREATE UNIQUE INDEX UX_DetalleCarrito_Personalizacion
        ON dbo.DetalleCarrito(IdPersonalizacion)
        WHERE IdPersonalizacion IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DetalleCarrito_VarianteSinPersonalizacion' AND object_id = OBJECT_ID(N'dbo.DetalleCarrito'))
        CREATE UNIQUE INDEX UX_DetalleCarrito_VarianteSinPersonalizacion
        ON dbo.DetalleCarrito(IdCarrito, IdVariante)
        WHERE IdPersonalizacion IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Orden_CarritoOrigen' AND object_id = OBJECT_ID(N'dbo.Orden'))
        CREATE UNIQUE INDEX UX_Orden_CarritoOrigen
        ON dbo.Orden(IdCarritoOrigen)
        WHERE IdCarritoOrigen IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Orden_ArchivoConstancia' AND object_id = OBJECT_ID(N'dbo.Orden'))
        CREATE UNIQUE INDEX UX_Orden_ArchivoConstancia
        ON dbo.Orden(IdArchivoConstancia)
        WHERE IdArchivoConstancia IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DetalleOrden_Personalizacion' AND object_id = OBJECT_ID(N'dbo.DetalleOrden'))
        CREATE UNIQUE INDEX UX_DetalleOrden_Personalizacion
        ON dbo.DetalleOrden(IdPersonalizacion)
        WHERE IdPersonalizacion IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DetalleOrden_VarianteSinPersonalizacion' AND object_id = OBJECT_ID(N'dbo.DetalleOrden'))
        CREATE UNIQUE INDEX UX_DetalleOrden_VarianteSinPersonalizacion
        ON dbo.DetalleOrden(IdOrden, IdVariante)
        WHERE IdPersonalizacion IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Pago_ReferenciaTransaccion' AND object_id = OBJECT_ID(N'dbo.Pago'))
        CREATE UNIQUE INDEX UX_Pago_ReferenciaTransaccion
        ON dbo.Pago(ReferenciaTransaccion)
        WHERE ReferenciaTransaccion IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_IntentoEntrega_Orden_Abierto' AND object_id = OBJECT_ID(N'dbo.IntentoEntrega'))
        CREATE UNIQUE INDEX UX_IntentoEntrega_Orden_Abierto
        ON dbo.IntentoEntrega(IdOrden)
        WHERE FechaHoraFin IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orden_Comprador' AND object_id = OBJECT_ID(N'dbo.Orden'))
        CREATE INDEX IX_Orden_Comprador ON dbo.Orden(IdCompradorExterno);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orden_Estado' AND object_id = OBJECT_ID(N'dbo.Orden'))
        CREATE INDEX IX_Orden_Estado ON dbo.Orden(IdEstadoOrdenActual);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orden_FechaCreacion' AND object_id = OBJECT_ID(N'dbo.Orden'))
        CREATE INDEX IX_Orden_FechaCreacion ON dbo.Orden(FechaCreacion);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orden_AreaEntrega' AND object_id = OBJECT_ID(N'dbo.Orden'))
        CREATE INDEX IX_Orden_AreaEntrega ON dbo.Orden(IdAreaEntrega);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_Estado' AND object_id = OBJECT_ID(N'dbo.Pago'))
        CREATE INDEX IX_Pago_Estado ON dbo.Pago(IdEstadoPago);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pago_FechaPago' AND object_id = OBJECT_ID(N'dbo.Pago'))
        CREATE INDEX IX_Pago_FechaPago ON dbo.Pago(FechaPago);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IntentoEntrega_Orden' AND object_id = OBJECT_ID(N'dbo.IntentoEntrega'))
        CREATE INDEX IX_IntentoEntrega_Orden ON dbo.IntentoEntrega(IdOrden);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IntentoEntrega_Repartidor' AND object_id = OBJECT_ID(N'dbo.IntentoEntrega'))
        CREATE INDEX IX_IntentoEntrega_Repartidor ON dbo.IntentoEntrega(IdRepartidor);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HistorialEstadoOrden_Orden_Fecha' AND object_id = OBJECT_ID(N'dbo.HistorialEstadoOrden'))
        CREATE INDEX IX_HistorialEstadoOrden_Orden_Fecha
        ON dbo.HistorialEstadoOrden(IdOrden, FechaHora);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notificacion_Orden' AND object_id = OBJECT_ID(N'dbo.Notificacion'))
        CREATE INDEX IX_Notificacion_Orden ON dbo.Notificacion(IdOrden);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BitacoraAuditoria_FechaHora' AND object_id = OBJECT_ID(N'dbo.BitacoraAuditoria'))
        CREATE INDEX IX_BitacoraAuditoria_FechaHora ON dbo.BitacoraAuditoria(FechaHora);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BitacoraAuditoria_UsuarioInterno' AND object_id = OBJECT_ID(N'dbo.BitacoraAuditoria'))
        CREATE INDEX IX_BitacoraAuditoria_UsuarioInterno
        ON dbo.BitacoraAuditoria(IdUsuarioInterno)
        WHERE IdUsuarioInterno IS NOT NULL;

    /* ============================================================
       8. SEEDS DE CATALOGOS FIJOS
       ============================================================ */

    SET IDENTITY_INSERT dbo.Rol ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = 1)
        INSERT dbo.Rol (IdRol, Codigo, Nombre) VALUES (1, N'ADMIN', N'Administrador');
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = 2)
        INSERT dbo.Rol (IdRol, Codigo, Nombre) VALUES (2, N'SUPERVISOR', N'Supervisor');
    IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE IdRol = 3)
        INSERT dbo.Rol (IdRol, Codigo, Nombre) VALUES (3, N'REPARTIDOR', N'Repartidor');
    SET IDENTITY_INSERT dbo.Rol OFF;

    SET IDENTITY_INSERT dbo.EstadoOrden ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE IdEstadoOrden = 1)
        INSERT dbo.EstadoOrden (IdEstadoOrden, Codigo, Nombre) VALUES (1, N'ORDEN_GENERADA', N'Orden generada');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE IdEstadoOrden = 2)
        INSERT dbo.EstadoOrden (IdEstadoOrden, Codigo, Nombre) VALUES (2, N'EN_ELABORACION', N'En elaboracion');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE IdEstadoOrden = 3)
        INSERT dbo.EstadoOrden (IdEstadoOrden, Codigo, Nombre) VALUES (3, N'LISTO_PARA_ENTREGA', N'Listo para entrega');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE IdEstadoOrden = 4)
        INSERT dbo.EstadoOrden (IdEstadoOrden, Codigo, Nombre) VALUES (4, N'EN_ENTREGA', N'En entrega');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE IdEstadoOrden = 5)
        INSERT dbo.EstadoOrden (IdEstadoOrden, Codigo, Nombre) VALUES (5, N'ENTREGADO', N'Entregado');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE IdEstadoOrden = 6)
        INSERT dbo.EstadoOrden (IdEstadoOrden, Codigo, Nombre) VALUES (6, N'COMPRADOR_NO_ENCONTRADO', N'Comprador no encontrado');
    SET IDENTITY_INSERT dbo.EstadoOrden OFF;

    SET IDENTITY_INSERT dbo.EstadoCarrito ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoCarrito WHERE IdEstadoCarrito = 1)
        INSERT dbo.EstadoCarrito (IdEstadoCarrito, Codigo, Nombre) VALUES (1, N'ACTIVO', N'Activo');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoCarrito WHERE IdEstadoCarrito = 2)
        INSERT dbo.EstadoCarrito (IdEstadoCarrito, Codigo, Nombre) VALUES (2, N'PROCESADO', N'Procesado');
    SET IDENTITY_INSERT dbo.EstadoCarrito OFF;

    SET IDENTITY_INSERT dbo.MetodoPago ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.MetodoPago WHERE IdMetodoPago = 1)
        INSERT dbo.MetodoPago (IdMetodoPago, Codigo, Nombre) VALUES (1, N'EFECTIVO', N'Efectivo');
    IF NOT EXISTS (SELECT 1 FROM dbo.MetodoPago WHERE IdMetodoPago = 2)
        INSERT dbo.MetodoPago (IdMetodoPago, Codigo, Nombre) VALUES (2, N'TARJETA', N'Tarjeta');
    SET IDENTITY_INSERT dbo.MetodoPago OFF;

    SET IDENTITY_INSERT dbo.EstadoPago ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoPago WHERE IdEstadoPago = 1)
        INSERT dbo.EstadoPago (IdEstadoPago, Codigo, Nombre) VALUES (1, N'PENDIENTE', N'Pendiente');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoPago WHERE IdEstadoPago = 2)
        INSERT dbo.EstadoPago (IdEstadoPago, Codigo, Nombre) VALUES (2, N'PAGADO', N'Pagado');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoPago WHERE IdEstadoPago = 3)
        INSERT dbo.EstadoPago (IdEstadoPago, Codigo, Nombre) VALUES (3, N'RECHAZADO', N'Rechazado');
    SET IDENTITY_INSERT dbo.EstadoPago OFF;

    SET IDENTITY_INSERT dbo.ResultadoEntrega ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.ResultadoEntrega WHERE IdResultadoEntrega = 1)
        INSERT dbo.ResultadoEntrega (IdResultadoEntrega, Codigo, Nombre) VALUES (1, N'ENTREGADO', N'Entregado');
    IF NOT EXISTS (SELECT 1 FROM dbo.ResultadoEntrega WHERE IdResultadoEntrega = 2)
        INSERT dbo.ResultadoEntrega (IdResultadoEntrega, Codigo, Nombre) VALUES (2, N'COMPRADOR_NO_ENCONTRADO', N'Comprador no encontrado');
    IF NOT EXISTS (SELECT 1 FROM dbo.ResultadoEntrega WHERE IdResultadoEntrega = 3)
        INSERT dbo.ResultadoEntrega (IdResultadoEntrega, Codigo, Nombre) VALUES (3, N'PAGO_NO_REALIZADO', N'Pago no realizado');
    SET IDENTITY_INSERT dbo.ResultadoEntrega OFF;

    SET IDENTITY_INSERT dbo.TipoActor ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.TipoActor WHERE IdTipoActor = 1)
        INSERT dbo.TipoActor (IdTipoActor, Codigo, Nombre) VALUES (1, N'SISTEMA', N'Sistema');
    IF NOT EXISTS (SELECT 1 FROM dbo.TipoActor WHERE IdTipoActor = 2)
        INSERT dbo.TipoActor (IdTipoActor, Codigo, Nombre) VALUES (2, N'USUARIO_INTERNO', N'Usuario interno');
    IF NOT EXISTS (SELECT 1 FROM dbo.TipoActor WHERE IdTipoActor = 3)
        INSERT dbo.TipoActor (IdTipoActor, Codigo, Nombre) VALUES (3, N'COMPRADOR', N'Comprador');
    SET IDENTITY_INSERT dbo.TipoActor OFF;

    SET IDENTITY_INSERT dbo.TipoNotificacion ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.TipoNotificacion WHERE IdTipoNotificacion = 1)
        INSERT dbo.TipoNotificacion (IdTipoNotificacion, Codigo, Nombre) VALUES (1, N'CONFIRMACION_COMPRA', N'Confirmacion de compra');
    IF NOT EXISTS (SELECT 1 FROM dbo.TipoNotificacion WHERE IdTipoNotificacion = 2)
        INSERT dbo.TipoNotificacion (IdTipoNotificacion, Codigo, Nombre) VALUES (2, N'PEDIDO_LISTO', N'Pedido listo');
    IF NOT EXISTS (SELECT 1 FROM dbo.TipoNotificacion WHERE IdTipoNotificacion = 3)
        INSERT dbo.TipoNotificacion (IdTipoNotificacion, Codigo, Nombre) VALUES (3, N'ENTREGA_CONFIRMADA', N'Entrega confirmada');
    SET IDENTITY_INSERT dbo.TipoNotificacion OFF;

    SET IDENTITY_INSERT dbo.CanalNotificacion ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.CanalNotificacion WHERE IdCanalNotificacion = 1)
        INSERT dbo.CanalNotificacion (IdCanalNotificacion, Codigo, Nombre) VALUES (1, N'CORREO', N'Correo');
    IF NOT EXISTS (SELECT 1 FROM dbo.CanalNotificacion WHERE IdCanalNotificacion = 2)
        INSERT dbo.CanalNotificacion (IdCanalNotificacion, Codigo, Nombre) VALUES (2, N'WHATSAPP', N'WhatsApp');
    SET IDENTITY_INSERT dbo.CanalNotificacion OFF;

    SET IDENTITY_INSERT dbo.EstadoNotificacion ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoNotificacion WHERE IdEstadoNotificacion = 1)
        INSERT dbo.EstadoNotificacion (IdEstadoNotificacion, Codigo, Nombre) VALUES (1, N'PENDIENTE', N'Pendiente');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoNotificacion WHERE IdEstadoNotificacion = 2)
        INSERT dbo.EstadoNotificacion (IdEstadoNotificacion, Codigo, Nombre) VALUES (2, N'ENVIADA', N'Enviada');
    IF NOT EXISTS (SELECT 1 FROM dbo.EstadoNotificacion WHERE IdEstadoNotificacion = 3)
        INSERT dbo.EstadoNotificacion (IdEstadoNotificacion, Codigo, Nombre) VALUES (3, N'FALLIDA', N'Fallida');
    SET IDENTITY_INSERT dbo.EstadoNotificacion OFF;

    /* ============================================================
       9. ADMIN INICIAL
       ============================================================ */

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.UsuarioInterno
        WHERE Correo = N'correonexttechsolution933@gmail.com'
    )
    BEGIN
        INSERT dbo.UsuarioInterno
        (
            IdRol,
            Nombres,
            Apellidos,
            Correo,
            PasswordHash,
            Activo,
            DebeCambiarPassword,
            IntentosFallidos
        )
        VALUES
        (
            1,
            N'Administrador',
            N'NextTech',
            N'correonexttechsolution933@gmail.com',
            N'AQAAAAIAAYagAAAAEB9PMf/fY7jRaIiRs8IFQ1rtordRT4AsRjGRNSXx6l3Mkrz6Frf2q1+N7BG1lgDkFg==',
            1,
            1,
            0
        );
    END;

    /* ============================================================
       10. SEED INICIAL DEL CATALOGO
       ============================================================ */

    IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE Nombre = N'Llaveros')
    BEGIN
        INSERT dbo.Categoria (Nombre, Descripcion)
        VALUES (N'Llaveros', N'Productos personalizados tipo llavero.');
    END;

    DECLARE @IdCategoriaLlaveros INT =
        (SELECT TOP (1) IdCategoria FROM dbo.Categoria WHERE Nombre = N'Llaveros');

    IF NOT EXISTS (SELECT 1 FROM dbo.Producto WHERE CodigoProducto = N'LLV-001')
    BEGIN
        INSERT dbo.Producto
        (
            IdCategoria,
            CodigoProducto,
            Nombre,
            Descripcion,
            PrecioBase,
            PermitePersonalizacion
        )
        VALUES
        (
            @IdCategoriaLlaveros,
            N'LLV-001',
            N'Llavero Personalizado',
            N'Llavero personalizable con zonas Lado A y Lado B.',
            20.00,
            1
        );
    END;

    DECLARE @IdProductoLlaveros INT =
        (SELECT TOP (1) IdProducto FROM dbo.Producto WHERE CodigoProducto = N'LLV-001');

    IF NOT EXISTS (SELECT 1 FROM dbo.Atributo WHERE Nombre = N'Material')
        INSERT dbo.Atributo (Nombre) VALUES (N'Material');

    IF NOT EXISTS (SELECT 1 FROM dbo.Atributo WHERE Nombre = N'Forma')
        INSERT dbo.Atributo (Nombre) VALUES (N'Forma');

    IF NOT EXISTS (SELECT 1 FROM dbo.Atributo WHERE Nombre = N'Tamano')
        INSERT dbo.Atributo (Nombre) VALUES (N'Tamano');

    DECLARE @IdAtributoMaterial INT = (SELECT IdAtributo FROM dbo.Atributo WHERE Nombre = N'Material');
    DECLARE @IdAtributoForma INT = (SELECT IdAtributo FROM dbo.Atributo WHERE Nombre = N'Forma');
    DECLARE @IdAtributoTamano INT = (SELECT IdAtributo FROM dbo.Atributo WHERE Nombre = N'Tamano');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoMaterial AND Valor = N'Acrilico')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoMaterial, N'Acrilico');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoMaterial AND Valor = N'Metalico')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoMaterial, N'Metalico');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoForma AND Valor = N'Circular')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoForma, N'Circular');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoForma AND Valor = N'Cuadrado')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoForma, N'Cuadrado');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoTamano AND Valor = N'Pequeno')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoTamano, N'Pequeno');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoTamano AND Valor = N'Mediano')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoTamano, N'Mediano');

    IF NOT EXISTS (SELECT 1 FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoTamano AND Valor = N'Grande')
        INSERT dbo.ValorAtributo (IdAtributo, Valor) VALUES (@IdAtributoTamano, N'Grande');

    IF NOT EXISTS (SELECT 1 FROM dbo.ProductoAtributo WHERE IdProducto = @IdProductoLlaveros AND IdAtributo = @IdAtributoMaterial)
        INSERT dbo.ProductoAtributo (IdProducto, IdAtributo, EsObligatorio, OrdenVisual)
        VALUES (@IdProductoLlaveros, @IdAtributoMaterial, 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.ProductoAtributo WHERE IdProducto = @IdProductoLlaveros AND IdAtributo = @IdAtributoForma)
        INSERT dbo.ProductoAtributo (IdProducto, IdAtributo, EsObligatorio, OrdenVisual)
        VALUES (@IdProductoLlaveros, @IdAtributoForma, 1, 2);

    IF NOT EXISTS (SELECT 1 FROM dbo.ProductoAtributo WHERE IdProducto = @IdProductoLlaveros AND IdAtributo = @IdAtributoTamano)
        INSERT dbo.ProductoAtributo (IdProducto, IdAtributo, EsObligatorio, OrdenVisual)
        VALUES (@IdProductoLlaveros, @IdAtributoTamano, 1, 3);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-ACR-CIR-MED')
        INSERT dbo.VarianteProducto (IdProducto, CodigoVariante, Nombre, PrecioAdicional)
        VALUES (@IdProductoLlaveros, N'LLV-ACR-CIR-MED', N'Acrilico Circular Mediano', 0.00);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-ACR-CUA-MED')
        INSERT dbo.VarianteProducto (IdProducto, CodigoVariante, Nombre, PrecioAdicional)
        VALUES (@IdProductoLlaveros, N'LLV-ACR-CUA-MED', N'Acrilico Cuadrado Mediano', 0.00);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-MET-CIR-MED')
        INSERT dbo.VarianteProducto (IdProducto, CodigoVariante, Nombre, PrecioAdicional)
        VALUES (@IdProductoLlaveros, N'LLV-MET-CIR-MED', N'Metalico Circular Mediano', 0.00);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-MET-CUA-MED')
        INSERT dbo.VarianteProducto (IdProducto, CodigoVariante, Nombre, PrecioAdicional)
        VALUES (@IdProductoLlaveros, N'LLV-MET-CUA-MED', N'Metalico Cuadrado Mediano', 0.00);

    DECLARE @IdValorAcrilico INT =
        (SELECT IdValorAtributo FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoMaterial AND Valor = N'Acrilico');
    DECLARE @IdValorMetalico INT =
        (SELECT IdValorAtributo FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoMaterial AND Valor = N'Metalico');
    DECLARE @IdValorCircular INT =
        (SELECT IdValorAtributo FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoForma AND Valor = N'Circular');
    DECLARE @IdValorCuadrado INT =
        (SELECT IdValorAtributo FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoForma AND Valor = N'Cuadrado');
    DECLARE @IdValorMediano INT =
        (SELECT IdValorAtributo FROM dbo.ValorAtributo WHERE IdAtributo = @IdAtributoTamano AND Valor = N'Mediano');

    DECLARE @IdVarAcrCirMed INT = (SELECT IdVariante FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-ACR-CIR-MED');
    DECLARE @IdVarAcrCuaMed INT = (SELECT IdVariante FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-ACR-CUA-MED');
    DECLARE @IdVarMetCirMed INT = (SELECT IdVariante FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-MET-CIR-MED');
    DECLARE @IdVarMetCuaMed INT = (SELECT IdVariante FROM dbo.VarianteProducto WHERE CodigoVariante = N'LLV-MET-CUA-MED');

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarAcrCirMed AND IdAtributo = @IdAtributoMaterial)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarAcrCirMed, @IdAtributoMaterial, @IdValorAcrilico);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarAcrCirMed AND IdAtributo = @IdAtributoForma)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarAcrCirMed, @IdAtributoForma, @IdValorCircular);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarAcrCirMed AND IdAtributo = @IdAtributoTamano)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarAcrCirMed, @IdAtributoTamano, @IdValorMediano);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarAcrCuaMed AND IdAtributo = @IdAtributoMaterial)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarAcrCuaMed, @IdAtributoMaterial, @IdValorAcrilico);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarAcrCuaMed AND IdAtributo = @IdAtributoForma)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarAcrCuaMed, @IdAtributoForma, @IdValorCuadrado);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarAcrCuaMed AND IdAtributo = @IdAtributoTamano)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarAcrCuaMed, @IdAtributoTamano, @IdValorMediano);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarMetCirMed AND IdAtributo = @IdAtributoMaterial)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarMetCirMed, @IdAtributoMaterial, @IdValorMetalico);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarMetCirMed AND IdAtributo = @IdAtributoForma)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarMetCirMed, @IdAtributoForma, @IdValorCircular);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarMetCirMed AND IdAtributo = @IdAtributoTamano)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarMetCirMed, @IdAtributoTamano, @IdValorMediano);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarMetCuaMed AND IdAtributo = @IdAtributoMaterial)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarMetCuaMed, @IdAtributoMaterial, @IdValorMetalico);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarMetCuaMed AND IdAtributo = @IdAtributoForma)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarMetCuaMed, @IdAtributoForma, @IdValorCuadrado);

    IF NOT EXISTS (SELECT 1 FROM dbo.VarianteAtributo WHERE IdVariante = @IdVarMetCuaMed AND IdAtributo = @IdAtributoTamano)
        INSERT dbo.VarianteAtributo (IdVariante, IdAtributo, IdValorAtributo)
        VALUES (@IdVarMetCuaMed, @IdAtributoTamano, @IdValorMediano);

    IF NOT EXISTS (SELECT 1 FROM dbo.ZonaPersonalizacion WHERE IdProducto = @IdProductoLlaveros AND Nombre = N'Lado A')
        INSERT dbo.ZonaPersonalizacion (IdProducto, Nombre, EsObligatoria, OrdenVisual)
        VALUES (@IdProductoLlaveros, N'Lado A', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.ZonaPersonalizacion WHERE IdProducto = @IdProductoLlaveros AND Nombre = N'Lado B')
        INSERT dbo.ZonaPersonalizacion (IdProducto, Nombre, EsObligatoria, OrdenVisual)
        VALUES (@IdProductoLlaveros, N'Lado B', 1, 2);

    DECLARE @IdZonaA INT =
        (SELECT IdZona FROM dbo.ZonaPersonalizacion WHERE IdProducto = @IdProductoLlaveros AND Nombre = N'Lado A');
    DECLARE @IdZonaB INT =
        (SELECT IdZona FROM dbo.ZonaPersonalizacion WHERE IdProducto = @IdProductoLlaveros AND Nombre = N'Lado B');

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarAcrCirMed AND IdZona = @IdZonaA)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarAcrCirMed, @IdZonaA, N'CIRCULAR', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarAcrCirMed AND IdZona = @IdZonaB)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarAcrCirMed, @IdZonaB, N'CIRCULAR', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarAcrCuaMed AND IdZona = @IdZonaA)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarAcrCuaMed, @IdZonaA, N'CUADRADA', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarAcrCuaMed AND IdZona = @IdZonaB)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarAcrCuaMed, @IdZonaB, N'CUADRADA', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarMetCirMed AND IdZona = @IdZonaA)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarMetCirMed, @IdZonaA, N'CIRCULAR', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarMetCirMed AND IdZona = @IdZonaB)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarMetCirMed, @IdZonaB, N'CIRCULAR', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarMetCuaMed AND IdZona = @IdZonaA)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarMetCuaMed, @IdZonaA, N'CUADRADA', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.PlantillaVarianteZona WHERE IdVariante = @IdVarMetCuaMed AND IdZona = @IdZonaB)
        INSERT dbo.PlantillaVarianteZona (IdVariante, IdZona, Forma, AnchoLienzo, AltoLienzo)
        VALUES (@IdVarMetCuaMed, @IdZonaB, N'CUADRADA', 800, 800);

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaEntrega WHERE Nombre = N'Entrada principal')
        INSERT dbo.AreaEntrega (Nombre, Descripcion)
        VALUES (N'Entrada principal', N'Punto de entrega en la entrada principal.');

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaEntrega WHERE Nombre = N'Cafeteria')
        INSERT dbo.AreaEntrega (Nombre, Descripcion)
        VALUES (N'Cafeteria', N'Punto de entrega en el area de cafeteria.');

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaEntrega WHERE Nombre = N'Biblioteca')
        INSERT dbo.AreaEntrega (Nombre, Descripcion)
        VALUES (N'Biblioteca', N'Punto de entrega en el area de biblioteca.');

    IF NOT EXISTS (SELECT 1 FROM dbo.AreaEntrega WHERE Nombre = N'Edificio principal')
        INSERT dbo.AreaEntrega (Nombre, Descripcion)
        VALUES (N'Edificio principal', N'Punto de entrega en el edificio principal.');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

/* ============================================================
   10.1 BIOMETRIA DE COMPRADORES (ORACLE EXTERNO)
   ============================================================ */

IF OBJECT_ID(N'dbo.BiometriaComprador', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BiometriaComprador
    (
        IdBiometriaComprador BIGINT IDENTITY(1,1) NOT NULL,
        IdCompradorExterno BIGINT NOT NULL,
        BiometricTemplate NVARCHAR(MAX) NOT NULL,
        TemplateVersion NVARCHAR(20) NOT NULL,
        TemplateKeyId NVARCHAR(100) NOT NULL,
        TemplateModel NVARCHAR(100) NOT NULL,
        TemplateDimensions INT NOT NULL,
        EmbeddingSha256 CHAR(64) NULL,
        RetratoDatos VARBINARY(MAX) NOT NULL,
        RetratoMime NVARCHAR(100) NOT NULL,
        RetratoAncho INT NOT NULL,
        RetratoAlto INT NOT NULL,
        RetratoFondo NVARCHAR(50) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_BiometriaComprador_Activo DEFAULT (1),
        FechaEnrolamiento DATETIME2(0) NOT NULL
            CONSTRAINT DF_BiometriaComprador_FechaEnrolamiento DEFAULT SYSUTCDATETIME(),
        FechaActualizacion DATETIME2(0) NULL,
        CONSTRAINT PK_BiometriaComprador PRIMARY KEY (IdBiometriaComprador),
        CONSTRAINT UQ_BiometriaComprador_IdCompradorExterno UNIQUE (IdCompradorExterno),
        CONSTRAINT CK_BiometriaComprador_TemplateDimensions CHECK (TemplateDimensions > 0),
        CONSTRAINT CK_BiometriaComprador_RetratoDimensiones CHECK (RetratoAncho > 0 AND RetratoAlto > 0)
    );
END;

/* ============================================================
   11. CONSULTAS DE VERIFICACION
   ============================================================ */

SELECT DB_NAME() AS BaseDeDatosActual;

SELECT IdRol, Codigo, Nombre
FROM dbo.Rol
ORDER BY IdRol;

SELECT IdEstadoOrden, Codigo, Nombre
FROM dbo.EstadoOrden
ORDER BY IdEstadoOrden;

SELECT
    IdUsuarioInterno,
    IdRol,
    Nombres,
    Apellidos,
    Correo,
    Activo,
    DebeCambiarPassword
FROM dbo.UsuarioInterno
WHERE Correo = N'correonexttechsolution933@gmail.com';

SELECT
    p.IdProducto,
    p.CodigoProducto,
    p.Nombre,
    p.PrecioBase,
    p.PermitePersonalizacion,
    p.Activo
FROM dbo.Producto p
WHERE p.CodigoProducto = N'LLV-001';

SELECT
    v.IdVariante,
    v.CodigoVariante,
    v.Nombre,
    v.PrecioAdicional,
    CAST(p.PrecioBase + v.PrecioAdicional AS DECIMAL(10,2)) AS PrecioActual
FROM dbo.VarianteProducto v
INNER JOIN dbo.Producto p ON p.IdProducto = v.IdProducto
WHERE p.CodigoProducto = N'LLV-001'
ORDER BY v.CodigoVariante;

SELECT
    a.Nombre AS Atributo,
    va.Valor
FROM dbo.ValorAtributo va
INNER JOIN dbo.Atributo a ON a.IdAtributo = va.IdAtributo
ORDER BY a.Nombre, va.Valor;

SELECT
    ae.IdAreaEntrega,
    ae.Nombre,
    ae.Descripcion,
    ae.Activo
FROM dbo.AreaEntrega ae
ORDER BY ae.IdAreaEntrega;

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo'
  AND TABLE_NAME = N'Orden'
  AND COLUMN_NAME IN (N'NombreCompradorAplicado', N'NicknameCompradorAplicado');
