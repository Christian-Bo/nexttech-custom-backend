SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

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

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
