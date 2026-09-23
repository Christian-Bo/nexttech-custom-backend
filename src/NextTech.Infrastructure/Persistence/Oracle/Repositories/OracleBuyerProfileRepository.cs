using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Application.Profiles;
using Oracle.ManagedDataAccess.Client;

namespace NextTech.Infrastructure.Persistence.Oracle.Repositories;

public sealed class OracleBuyerProfileRepository(OracleDbContext db) : IBuyerProfileRepository
{
    public async Task<BuyerProfileData?> GetAsync(long buyerId, CancellationToken ct)
    {
        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT
                U.ID_USUARIO,
                U.CORREO,
                U.TELEFONO,
                U.FECHA_NACIMIENTO,
                U.NICKNAME,
                U.NOTIFICA_EMAIL,
                U.NOTIFICA_WHATSAPP,
                U.ACTIVO,
                U.BLOQUEADO,
                CASE WHEN AO.ID_ARCHIVO IS NOT NULL AND AO.ACTIVO = 'S' THEN 'S' ELSE 'N' END AS TIENE_FOTO_ORIGINAL,
                CASE WHEN AM.ID_ARCHIVO IS NOT NULL AND AM.ACTIVO = 'S' THEN 'S' ELSE 'N' END AS TIENE_FOTO_MODIFICADA,
                AM.MIME_TYPE,
                CAST(SYS_EXTRACT_UTC(AM.FECHA_CARGA) AS TIMESTAMP) AS FOTO_ACTUALIZADA_UTC,
                CAST(SYS_EXTRACT_UTC(U.ULTIMO_ACCESO) AS TIMESTAMP) AS ULTIMO_ACCESO_UTC,
                CAST(SYS_EXTRACT_UTC(U.FECHA_CREACION) AS TIMESTAMP) AS FECHA_CREACION_UTC,
                CAST(SYS_EXTRACT_UTC(U.FECHA_MODIFICACION) AS TIMESTAMP) AS FECHA_MODIFICACION_UTC
            FROM TIENDA_APP.USUARIO U
            LEFT JOIN TIENDA_APP.ARCHIVO AO
                ON AO.ID_ARCHIVO = U.ID_FOTO_ORIGINAL
               AND AO.ID_USUARIO_CARGA = U.ID_USUARIO
               AND AO.TIPO_ARCHIVO = 'FOTO_USUARIO_ORIGINAL'
            LEFT JOIN TIENDA_APP.ARCHIVO AM
                ON AM.ID_ARCHIVO = U.ID_FOTO_MODIFICADA
               AND AM.ID_USUARIO_CARGA = U.ID_USUARIO
               AND AM.TIPO_ARCHIVO = 'FOTO_USUARIO_MODIFICADA'
            WHERE U.ID_USUARIO = :id_usuario
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

        await using var reader = (OracleDataReader)await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new BuyerProfileData(
            ConvertOracleInt64(reader.GetValue(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetDateTime(3),
            reader.GetString(4),
            IsYes(reader.GetString(5)),
            IsYes(reader.GetString(6)),
            IsYes(reader.GetString(7)),
            IsYes(reader.GetString(8)),
            IsYes(reader.GetString(9)),
            IsYes(reader.GetString(10)),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            ReadUtcDateTimeOffset(reader, 12),
            ReadUtcDateTimeOffset(reader, 13),
            ReadUtcDateTimeOffset(reader, 14)
                ?? throw new AppDependencyException("Oracle no devolvió la fecha de creación del comprador."),
            ReadUtcDateTimeOffset(reader, 15)
                ?? throw new AppDependencyException("Oracle no devolvió la fecha de modificación del comprador."));
    }

    public async Task<bool> IsIdentifierInUseByOtherAsync(
        string identifier,
        long buyerId,
        CancellationToken ct)
    {
        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT COUNT(*)
            FROM TIENDA_APP.USUARIO U
            WHERE U.ID_USUARIO <> :id_usuario
              AND (LOWER(U.CORREO) = LOWER(:identificador)
                   OR LOWER(U.NICKNAME) = LOWER(:identificador)
                   OR U.TELEFONO = :identificador)
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);
        Add(command, "identificador", OracleDbType.Varchar2, identifier, 150);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
        return count > 0;
    }

    public async Task UpdateAsync(
        long buyerId,
        BuyerProfileUpdateData data,
        CancellationToken ct)
    {
        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            UPDATE TIENDA_APP.USUARIO
               SET TELEFONO = :telefono,
                   FECHA_NACIMIENTO = :fecha_nacimiento,
                   NICKNAME = :nickname,
                   NOTIFICA_EMAIL = :notifica_email,
                   NOTIFICA_WHATSAPP = :notifica_whatsapp,
                   FECHA_MODIFICACION = SYSTIMESTAMP
             WHERE ID_USUARIO = :id_usuario
            """;
        Add(command, "telefono", OracleDbType.Varchar2, data.Phone, 25);
        Add(command, "fecha_nacimiento", OracleDbType.Date, data.BirthDate?.Date);
        Add(command, "nickname", OracleDbType.Varchar2, data.Nickname, 50);
        Add(command, "notifica_email", OracleDbType.Char, data.NotifyByEmail ? "S" : "N", 1);
        Add(command, "notifica_whatsapp", OracleDbType.Char, data.NotifyByWhatsApp ? "S" : "N", 1);
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

        var affected = await command.ExecuteNonQueryAsync(ct);
        if (affected != 1)
            throw new AppNotFoundException("Comprador no encontrado.");
    }

    public async Task<BuyerDisplayPhoto?> GetDisplayPhotoAsync(long buyerId, CancellationToken ct)
    {
        var connection = GetConnection();
        await EnsureOpenAsync(connection, ct);

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = """
            SELECT
                A.CONTENIDO,
                A.MIME_TYPE,
                A.NOMBRE_ARCHIVO,
                A.HASH_SHA256,
                CAST(SYS_EXTRACT_UTC(A.FECHA_CARGA) AS TIMESTAMP) AS FECHA_CARGA_UTC
            FROM TIENDA_APP.USUARIO U
            INNER JOIN TIENDA_APP.ARCHIVO A
                ON A.ID_ARCHIVO = U.ID_FOTO_MODIFICADA
               AND A.ID_USUARIO_CARGA = U.ID_USUARIO
            WHERE U.ID_USUARIO = :id_usuario
              AND A.TIPO_ARCHIVO = 'FOTO_USUARIO_MODIFICADA'
              AND A.ACTIVO = 'S'
            """;
        Add(command, "id_usuario", OracleDbType.Int64, buyerId);

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

        return new BuyerDisplayPhoto(
            content,
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            ReadUtcDateTimeOffset(reader, 4)
                ?? throw new AppDependencyException("Oracle no devolvió la fecha de carga de la fotografía."));
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

    private static bool IsYes(string? value)
        => string.Equals(value?.Trim(), "S", StringComparison.OrdinalIgnoreCase);

    private static DateTimeOffset? ReadUtcDateTimeOffset(OracleDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static long ConvertOracleInt64(object value)
        => Convert.ToInt64(value.ToString(), CultureInfo.InvariantCulture);

    private static async Task EnsureOpenAsync(OracleConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
    }
}
