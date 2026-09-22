using Microsoft.EntityFrameworkCore;
using NextTech.Infrastructure.Persistence.Oracle.Models;

namespace NextTech.Infrastructure.Persistence.Oracle;

/// <summary>
/// Contexto de lectura EF para Oracle central. Las escrituras autorizadas de identidad
/// se realizan explícitamente mediante OracleCentralIdentityGateway; no se habilita
/// SaveChanges general sobre el esquema compartido.
/// </summary>
public sealed class OracleDbContext(DbContextOptions<OracleDbContext> options) : DbContext(options)
{
    public DbSet<UsuarioCentral> Usuarios => Set<UsuarioCentral>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var u = modelBuilder.Entity<UsuarioCentral>();
        u.ToTable("USUARIO", "TIENDA_APP");
        u.HasKey(x => x.IdUsuario);
        u.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO");
        u.Property(x => x.IdFotoOriginal).HasColumnName("ID_FOTO_ORIGINAL");
        u.Property(x => x.IdFotoModificada).HasColumnName("ID_FOTO_MODIFICADA");
        u.Property(x => x.Correo).HasColumnName("CORREO");
        u.Property(x => x.Telefono).HasColumnName("TELEFONO");
        u.Property(x => x.FechaNacimiento).HasColumnName("FECHA_NACIMIENTO");
        u.Property(x => x.Nickname).HasColumnName("NICKNAME");
        u.Property(x => x.PasswordHash).HasColumnName("PASSWORD_HASH");
        u.Property(x => x.TokenQrHash).HasColumnName("TOKEN_QR_HASH");
        u.Property(x => x.NotificaEmail).HasColumnName("NOTIFICA_EMAIL");
        u.Property(x => x.NotificaWhatsApp).HasColumnName("NOTIFICA_WHATSAPP");
        u.Property(x => x.Activo).HasColumnName("ACTIVO");
        u.Property(x => x.Bloqueado).HasColumnName("BLOQUEADO");
        u.Property(x => x.IntentosFallidos).HasColumnName("INTENTOS_FALLIDOS");
    }

    public override int SaveChanges() => throw ReadOnly();
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw ReadOnly();
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw ReadOnly();
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => throw ReadOnly();
    private static NotSupportedException ReadOnly() => new("OracleDbContext es de lectura. Use ICentralIdentityGateway para escrituras de identidad autorizadas.");
}
