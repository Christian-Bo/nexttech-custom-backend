using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Persistence.Oracle.Repositories;

public sealed class OracleCentralIdentityGateway(OracleDbContext db) : ICentralIdentityGateway
{
    public async Task<BuyerAuthRecord?> FindByIdentifierAsync(string identifier, CancellationToken ct)
    {
        var value = identifier.Trim();
        var normalized = value.ToLowerInvariant();

        // Oracle versions without SQL BOOLEAN support cannot safely translate a projection
        // such as x.Activo == "S" into a SELECT expression. Materialize the CHAR/VARCHAR2
        // values first and convert S/N to bool in managed code.
        var user = await db.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Correo.ToLower() == normalized ||
                x.Nickname.ToLower() == normalized ||
                x.Telefono == value, ct);

        return user is null ? null : ToBuyerAuthRecord(user);
    }

    public async Task<BuyerAuthRecord?> FindByIdAsync(long idUsuario, CancellationToken ct)
    {
        var user = await db.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdUsuario == idUsuario, ct);

        return user is null ? null : ToBuyerAuthRecord(user);
    }

    public async Task<BuyerAuthRecord?> FindByQrHashAsync(string qrHash, CancellationToken ct)
    {
        var user = await db.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenQrHash == qrHash, ct);

        return user is null ? null : ToBuyerAuthRecord(user);
    }

    public async Task<long> RegisterAsync(BuyerRegistrationData data, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO TIENDA_APP.USUARIO
(CORREO, TELEFONO, FECHA_NACIMIENTO, NICKNAME, PASSWORD_HASH, TOKEN_QR_HASH,
 NOTIFICA_EMAIL, NOTIFICA_WHATSAPP, ACTIVO, BLOQUEADO, INTENTOS_FALLIDOS)
VALUES (:correo, :telefono, :nacimiento, :nickname, :password_hash, :qr_hash,
 :notif_email, :notif_whatsapp, 'S', 'N', 0)
RETURNING ID_USUARIO INTO :id_usuario";
            Add(cmd, "correo", data.Correo, DbType.String);
            Add(cmd, "telefono", data.Telefono, DbType.String);
            Add(cmd, "nacimiento", data.FechaNacimiento, DbType.Date);
            Add(cmd, "nickname", data.Nickname, DbType.String);
            Add(cmd, "password_hash", data.PasswordHash, DbType.String);
            Add(cmd, "qr_hash", data.QrHash, DbType.String);
            Add(cmd, "notif_email", data.NotificaEmail ? "S" : "N", DbType.String);
            Add(cmd, "notif_whatsapp", data.NotificaWhatsApp ? "S" : "N", DbType.String);
            var output = Add(cmd, "id_usuario", null, DbType.Int64);
            output.Direction = ParameterDirection.Output;
            await cmd.ExecuteNonQueryAsync(ct);
            var id = Convert.ToInt64(output.Value);
            await tx.CommitAsync(ct);
            return id;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct)
    {
        var sql = unlock
            ? "UPDATE TIENDA_APP.USUARIO SET PASSWORD_HASH=:hash, BLOQUEADO='N', INTENTOS_FALLIDOS=0, FECHA_MODIFICACION=SYSTIMESTAMP WHERE ID_USUARIO=:id"
            : "UPDATE TIENDA_APP.USUARIO SET PASSWORD_HASH=:hash, FECHA_MODIFICACION=SYSTIMESTAMP WHERE ID_USUARIO=:id";
        await ExecuteAsync(sql, ct, ("hash", passwordHash, DbType.String), ("id", idUsuario, DbType.Int64));
    }

    public async Task RegisterFailedLoginAsync(long? idUsuario, string identifier, string method, string reason, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            if (idUsuario is not null)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = tx;
                update.CommandText = @"UPDATE TIENDA_APP.USUARIO
SET INTENTOS_FALLIDOS=INTENTOS_FALLIDOS+1,
    BLOQUEADO=CASE WHEN INTENTOS_FALLIDOS+1 >= 5 THEN 'S' ELSE BLOQUEADO END,
    FECHA_MODIFICACION=SYSTIMESTAMP
WHERE ID_USUARIO=:id";
                Add(update, "id", idUsuario.Value, DbType.Int64);
                await update.ExecuteNonQueryAsync(ct);
            }
            await InsertAuditAsync(connection, tx, idUsuario, identifier, method, false, reason, ct);
            await tx.CommitAsync(ct);
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    public async Task RegisterSuccessfulLoginAsync(long idUsuario, string identifier, string method, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            await using var update = connection.CreateCommand();
            update.Transaction = tx;
            update.CommandText = @"UPDATE TIENDA_APP.USUARIO SET INTENTOS_FALLIDOS=0, ULTIMO_ACCESO=SYSTIMESTAMP, FECHA_MODIFICACION=SYSTIMESTAMP WHERE ID_USUARIO=:id";
            Add(update, "id", idUsuario, DbType.Int64);
            await update.ExecuteNonQueryAsync(ct);
            await InsertAuditAsync(connection, tx, idUsuario, identifier, method, true, "Login OK", ct);
            await tx.CommitAsync(ct);
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    public async Task<bool> VerifyLegacyPasswordAsync(string identifier, string password, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "TIENDA_APP.PKG_LOGIN.CRUD";
        Add(cmd, "p_usuario_o_correo", identifier, DbType.String);
        Add(cmd, "p_nickname", null, DbType.String);
        Add(cmd, "p_password", password, DbType.String);
        Add(cmd, "p_token_qr", null, DbType.String);
        Add(cmd, "p_id_archivo_foto", null, DbType.Int64);
        Add(cmd, "p_opcion", "C", DbType.String);
        var code = Add(cmd, "p_codigo_s", null, DbType.Int32); code.Direction = ParameterDirection.Output;
        var message = Add(cmd, "p_mensaje", null, DbType.String); message.Direction = ParameterDirection.Output; message.Size = 2000;
        var data = Add(cmd, "p_data", null, DbType.String); data.Direction = ParameterDirection.Output; data.Size = 32767;
        await cmd.ExecuteNonQueryAsync(ct);
        return Convert.ToInt32(code.Value) == 200;
    }

    public Task SetQrHashAsync(long idUsuario, string qrHash, CancellationToken ct)
        => ExecuteAsync("UPDATE TIENDA_APP.USUARIO SET TOKEN_QR_HASH=:hash, FECHA_MODIFICACION=SYSTIMESTAMP WHERE ID_USUARIO=:id", ct,
            ("hash", qrHash, DbType.String), ("id", idUsuario, DbType.Int64));

    public async Task CreateRecoveryTokenAsync(long idUsuario, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await ExecuteAsync(@"UPDATE TIENDA_APP.TOKEN_RECUPERACION SET UTILIZADO='S', FECHA_USO=SYSTIMESTAMP WHERE ID_USUARIO=:id AND UTILIZADO='N'", ct,
            ("id", idUsuario, DbType.Int64));
        await ExecuteAsync(@"INSERT INTO TIENDA_APP.TOKEN_RECUPERACION (ID_USUARIO,TOKEN_HASH,FECHA_EXPIRACION,UTILIZADO) VALUES (:id,:hash,:expires,'N')", ct,
            ("id", idUsuario, DbType.Int64), ("hash", tokenHash, DbType.String), ("expires", expiresAt.UtcDateTime, DbType.DateTime));
    }

    public async Task<long?> FindValidRecoveryUserAsync(string tokenHash, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT ID_USUARIO FROM TIENDA_APP.TOKEN_RECUPERACION WHERE TOKEN_HASH=:hash AND UTILIZADO='N' AND FECHA_EXPIRACION>SYSTIMESTAMP";
        Add(cmd, "hash", tokenHash, DbType.String);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value is null || value == DBNull.Value ? null : Convert.ToInt64(value);
    }

    public Task ConsumeRecoveryTokenAsync(string tokenHash, CancellationToken ct)
        => ExecuteAsync("UPDATE TIENDA_APP.TOKEN_RECUPERACION SET UTILIZADO='S', FECHA_USO=SYSTIMESTAMP WHERE TOKEN_HASH=:hash AND UTILIZADO='N'", ct,
            ("hash", tokenHash, DbType.String));

    private static BuyerAuthRecord ToBuyerAuthRecord(Models.UsuarioCentral x)
        => new(
            x.IdUsuario, x.Correo, x.Telefono, x.FechaNacimiento, x.Nickname,
            x.PasswordHash, IsYes(x.NotificaEmail), IsYes(x.NotificaWhatsApp),
            IsYes(x.Activo), IsYes(x.Bloqueado), x.IntentosFallidos,
            x.IdFotoOriginal, x.IdFotoModificada);

    private static bool IsYes(string? value)
        => string.Equals(value?.Trim(), "S", StringComparison.OrdinalIgnoreCase);

    private async Task ExecuteAsync(string sql, CancellationToken ct, params (string name, object? value, DbType type)[] parameters)
    {
        var connection = db.Database.GetDbConnection();
        await EnsureOpenAsync(connection, ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in parameters) Add(cmd, p.name, p.value, p.type);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertAuditAsync(DbConnection connection, DbTransaction tx, long? idUsuario, string identifier, string method, bool ok, string reason, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"INSERT INTO TIENDA_APP.BITACORA_ACCESO (ID_USUARIO,IDENTIFICADOR,METODO_ACCESO,RESULTADO,MOTIVO) VALUES (:id,:ident,:method,:result,:reason)";
        Add(cmd, "id", idUsuario, DbType.Int64);
        Add(cmd, "ident", identifier.Length > 150 ? identifier[..150] : identifier, DbType.String);
        Add(cmd, "method", method, DbType.String);
        Add(cmd, "result", ok ? "S" : "N", DbType.String);
        Add(cmd, "reason", reason.Length > 300 ? reason[..300] : reason, DbType.String);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static DbParameter Add(DbCommand cmd, string name, object? value, DbType type)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.DbType = type;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
        return p;
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
    }


}
