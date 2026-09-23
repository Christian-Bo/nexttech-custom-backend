using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace NextTech.Infrastructure.Persistence.Oracle.Repositories;

/// <summary>
/// Persists facial enrollment using only the shared Oracle schema that already exists:
/// USUARIO.ID_FOTO_ORIGINAL / ID_FOTO_MODIFICADA and ARCHIVO.
/// Protected face templates are intentionally transient and are never persisted here.
/// </summary>
public sealed class OracleBuyerBiometricStore(OracleDbContext db) : IBuyerFaceEnrollmentStore
{
    public async Task<BuyerFaceEnrollment?> GetActiveAsync(long buyerId, CancellationToken ct)
    {
        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT
                AO.CONTENIDO,
                AO.MIME_TYPE,
                AO.NOMBRE_ARCHIVO,
                AM.CONTENIDO,
                AM.MIME_TYPE,
                CAST(SYS_EXTRACT_UTC(AO.FECHA_CARGA) AS TIMESTAMP) AS FECHA_ENROLAMIENTO_UTC
            FROM TIENDA_APP.USUARIO U
            INNER JOIN TIENDA_APP.ARCHIVO AO
                ON AO.ID_ARCHIVO = U.ID_FOTO_ORIGINAL
               AND AO.ID_USUARIO_CARGA = U.ID_USUARIO
            INNER JOIN TIENDA_APP.ARCHIVO AM
                ON AM.ID_ARCHIVO = U.ID_FOTO_MODIFICADA
               AND AM.ID_USUARIO_CARGA = U.ID_USUARIO
            WHERE U.ID_USUARIO = :id_usuario
              AND AO.ACTIVO = 'S'
              AND AM.ACTIVO = 'S'
              AND AO.TIPO_ARCHIVO = 'FOTO_USUARIO_ORIGINAL'
              AND AM.TIPO_ARCHIVO = 'FOTO_USUARIO_MODIFICADA'
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

        await using var reader = (OracleDataReader)await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            ct);

        if (!await reader.ReadAsync(ct))
            return null;

        byte[] originalContent;
        using (var originalBlob = reader.GetOracleBlob(0))
            originalContent = originalBlob.Value;

        var originalContentType = reader.GetString(1);
        var originalFileName = reader.GetString(2);

        byte[] portraitContent;
        using (var portraitBlob = reader.GetOracleBlob(3))
            portraitContent = portraitBlob.Value;

        var portraitContentType = reader.GetString(4);
        var enrolledAtUtc = ToUtc(reader.GetDateTime(5));

        if (originalContent.Length == 0 || portraitContent.Length == 0)
            return null;

        return new BuyerFaceEnrollment(
            buyerId,
            new FaceImage(originalContent, originalContentType, originalFileName),
            portraitContent,
            portraitContentType,
            enrolledAtUtc);
    }

    public async Task<BuyerFaceEnrollment> UpsertAsync(
        BuyerFaceEnrollment enrollment,
        CancellationToken ct)
    {
        if (enrollment.ReferenceImage.Content.Length == 0)
            throw new AppValidationException("La fotografía original no puede estar vacía.");
        if (enrollment.PortraitContent.Length == 0)
            throw new AppValidationException("El retrato procesado no puede estar vacío.");

        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            ct);

        try
        {
            var currentPhotos = await GetCurrentPhotoIdsAsync(
                connection,
                transaction,
                enrollment.BuyerId,
                ct);

            var originalPhotoId = await InsertPhotoAsync(
                connection,
                transaction,
                enrollment.BuyerId,
                "FOTO_USUARIO_ORIGINAL",
                enrollment.ReferenceImage.Content,
                enrollment.ReferenceImage.ContentType,
                CreateFileName(
                    enrollment.BuyerId,
                    "original",
                    enrollment.ReferenceImage.ContentType),
                ct);

            var modifiedPhotoId = await InsertPhotoAsync(
                connection,
                transaction,
                enrollment.BuyerId,
                "FOTO_USUARIO_MODIFICADA",
                enrollment.PortraitContent,
                enrollment.PortraitContentType,
                CreateFileName(
                    enrollment.BuyerId,
                    "modified",
                    enrollment.PortraitContentType),
                ct);

            await UpdateUserPhotosAsync(
                connection,
                transaction,
                enrollment.BuyerId,
                originalPhotoId,
                modifiedPhotoId,
                ct);

            if (currentPhotos.OriginalPhotoId is not null)
            {
                await DeactivatePhotoAsync(
                    connection,
                    transaction,
                    enrollment.BuyerId,
                    currentPhotos.OriginalPhotoId.Value,
                    ct);
            }

            if (currentPhotos.ModifiedPhotoId is not null &&
                currentPhotos.ModifiedPhotoId != currentPhotos.OriginalPhotoId)
            {
                await DeactivatePhotoAsync(
                    connection,
                    transaction,
                    enrollment.BuyerId,
                    currentPhotos.ModifiedPhotoId.Value,
                    ct);
            }

            await transaction.CommitAsync(ct);
            return enrollment;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private OracleConnection GetConnection()
        => (OracleConnection)db.Database.GetDbConnection();

    private static async Task<(long? OriginalPhotoId, long? ModifiedPhotoId)> GetCurrentPhotoIdsAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT ID_FOTO_ORIGINAL, ID_FOTO_MODIFICADA
            FROM TIENDA_APP.USUARIO
            WHERE ID_USUARIO = :id_usuario
            FOR UPDATE
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

        await using var reader = (OracleDataReader)await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new AppNotFoundException("Comprador no encontrado.");

        return (
            GetNullableInt64(reader, 0),
            GetNullableInt64(reader, 1));
    }

    private static async Task<long> InsertPhotoAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        string fileType,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            INSERT INTO TIENDA_APP.ARCHIVO
            (
                ID_USUARIO_CARGA,
                TIPO_ARCHIVO,
                NOMBRE_ARCHIVO,
                MIME_TYPE,
                TAMANO_BYTES,
                HASH_SHA256,
                CONTENIDO,
                ACTIVO
            )
            VALUES
            (
                :id_usuario,
                :tipo_archivo,
                :nombre_archivo,
                :mime_type,
                :tamano_bytes,
                :hash_sha256,
                :contenido,
                'S'
            )
            RETURNING ID_ARCHIVO INTO :id_archivo
            """;

        Add(command, "id_usuario", OracleDbType.Int64, buyerId);
        Add(command, "tipo_archivo", OracleDbType.Varchar2, fileType, 30);
        Add(command, "nombre_archivo", OracleDbType.Varchar2, fileName, 255);
        Add(command, "mime_type", OracleDbType.Varchar2, contentType, 100);
        Add(command, "tamano_bytes", OracleDbType.Int64, content.LongLength);
        Add(command, "hash_sha256", OracleDbType.Varchar2, HashBytes(content), 64);
        Add(command, "contenido", OracleDbType.Blob, content);

        var output = Add(command, "id_archivo", OracleDbType.Int64, null);
        output.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(ct);
        return ConvertOracleInt64(output.Value);
    }

    private static async Task UpdateUserPhotosAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        long originalPhotoId,
        long modifiedPhotoId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE TIENDA_APP.USUARIO
               SET ID_FOTO_ORIGINAL = :id_foto_original,
                   ID_FOTO_MODIFICADA = :id_foto_modificada,
                   FECHA_MODIFICACION = SYSTIMESTAMP
             WHERE ID_USUARIO = :id_usuario
            """;
        Add(command, "id_foto_original", OracleDbType.Int64, originalPhotoId);
        Add(command, "id_foto_modificada", OracleDbType.Int64, modifiedPhotoId);
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

        var affected = await command.ExecuteNonQueryAsync(ct);
        if (affected != 1)
            throw new AppNotFoundException("Comprador no encontrado.");
    }

    private static async Task DeactivatePhotoAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        long fileId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE TIENDA_APP.ARCHIVO
               SET ACTIVO = 'N'
             WHERE ID_ARCHIVO = :id_archivo
               AND ID_USUARIO_CARGA = :id_usuario
               AND TIPO_ARCHIVO IN ('FOTO_USUARIO_ORIGINAL', 'FOTO_USUARIO_MODIFICADA')
            """;
        Add(command, "id_archivo", OracleDbType.Int64, fileId);
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static OracleParameter Add(
        OracleCommand command,
        string name,
        OracleDbType type,
        object? value,
        int? size = null)
    {
        var parameter = command.Parameters.Add(name, type);
        if (size is not null)
            parameter.Size = size.Value;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }

    private static long? GetNullableInt64(OracleDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal)
            ? null
            : ConvertOracleInt64(reader.GetValue(ordinal));

    private static long ConvertOracleInt64(object value)
        => Convert.ToInt64(value.ToString(), CultureInfo.InvariantCulture);

    private static DateTimeOffset ToUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string HashBytes(byte[] content)
        => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static string CreateFileName(long buyerId, string kind, string contentType)
        => $"buyer-{buyerId}-{kind}{GetExtension(contentType)}";

    private static string GetExtension(string contentType)
        => contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };

    private static async Task EnsureOpenAsync(OracleConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
    }
}
