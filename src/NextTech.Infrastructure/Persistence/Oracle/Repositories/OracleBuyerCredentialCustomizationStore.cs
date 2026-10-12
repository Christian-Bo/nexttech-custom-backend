using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextTech.Application.Common;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace NextTech.Infrastructure.Persistence.Oracle.Repositories;

/// <summary>
/// Stores the buyer credential design as an application/json file in the existing
/// Oracle ARCHIVO table. This avoids adding a new database table while keeping the
/// visual configuration separate from the biometric reference image.
/// </summary>
public sealed class OracleBuyerCredentialCustomizationStore(OracleDbContext db)
    : IBuyerCredentialCustomizationStore
{
    private const string FileType = "CREDENCIAL_CONFIG";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CredentialCustomization?> GetAsync(long buyerId, CancellationToken ct)
    {
        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT CONTENIDO
            FROM TIENDA_APP.ARCHIVO
            WHERE ID_USUARIO_CARGA = :id_usuario
              AND TIPO_ARCHIVO = :tipo_archivo
              AND ACTIVO = 'S'
            ORDER BY FECHA_CARGA DESC, ID_ARCHIVO DESC
            FETCH FIRST 1 ROWS ONLY
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);
        Add(command, "tipo_archivo", OracleDbType.Varchar2, FileType, 30);

        await using var reader = (OracleDataReader)await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            ct);

        if (!await reader.ReadAsync(ct))
            return null;

        byte[] content;
        using (var blob = reader.GetOracleBlob(0))
            content = blob.Value;

        if (content.Length == 0)
            return null;

        try
        {
            return JsonSerializer.Deserialize<CredentialCustomization>(content, JsonOptions);
        }
        catch (JsonException)
        {
            throw new AppDependencyException("La configuración almacenada de la credencial no es válida.");
        }
    }

    public async Task UpsertAsync(
        long buyerId,
        CredentialCustomization customization,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(customization);

        var content = JsonSerializer.SerializeToUtf8Bytes(customization, JsonOptions);
        if (content.Length > 64 * 1024)
            throw new AppValidationException("La configuración de la credencial excede el tamaño permitido.");

        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);
        await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            ct);

        try
        {
            await EnsureBuyerExistsAsync(connection, transaction, buyerId, ct);
            await DeactivatePreviousAsync(connection, transaction, buyerId, ct);
            await InsertAsync(connection, transaction, buyerId, content, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task EnsureBuyerExistsAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            SELECT 1
            FROM TIENDA_APP.USUARIO
            WHERE ID_USUARIO = :id_usuario
            FOR UPDATE
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

        var exists = await command.ExecuteScalarAsync(ct);
        if (exists is null || exists is DBNull)
            throw new AppNotFoundException("Comprador no encontrado.");
    }

    private static async Task DeactivatePreviousAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;
        command.CommandText = """
            UPDATE TIENDA_APP.ARCHIVO
               SET ACTIVO = 'N'
             WHERE ID_USUARIO_CARGA = :id_usuario
               AND TIPO_ARCHIVO = :tipo_archivo
               AND ACTIVO = 'S'
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);
        Add(command, "tipo_archivo", OracleDbType.Varchar2, FileType, 30);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        long buyerId,
        byte[] content,
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
            """;

        Add(command, "id_usuario", OracleDbType.Int64, buyerId);
        Add(command, "tipo_archivo", OracleDbType.Varchar2, FileType, 30);
        Add(command, "nombre_archivo", OracleDbType.Varchar2, $"buyer-{buyerId}-credential-config.json", 255);
        Add(command, "mime_type", OracleDbType.Varchar2, "application/json", 100);
        Add(command, "tamano_bytes", OracleDbType.Int64, content.LongLength);
        Add(command, "hash_sha256", OracleDbType.Varchar2,
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(), 64);
        Add(command, "contenido", OracleDbType.Blob, content);

        await command.ExecuteNonQueryAsync(ct);
    }

    private OracleConnection GetConnection()
        => (OracleConnection)db.Database.GetDbConnection();

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

    private static async Task EnsureOpenAsync(OracleConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
    }
}
