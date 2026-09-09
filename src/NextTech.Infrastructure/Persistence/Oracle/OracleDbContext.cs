using Microsoft.EntityFrameworkCore;
using NextTech.Infrastructure.Persistence.Oracle.Models;

namespace NextTech.Infrastructure.Persistence.Oracle;

/// <summary>
/// Contexto de solo lectura para la base Oracle central.
/// NextTech NO administra el esquema Oracle y NO debe generar migraciones sobre él.
/// </summary>
public sealed class OracleDbContext(DbContextOptions<OracleDbContext> options)
    : DbContext(options)
{
    public DbSet<UsuarioCentral> Usuarios => Set<UsuarioCentral>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var usuario = modelBuilder.Entity<UsuarioCentral>();

        usuario.ToTable("USUARIO", "TIENDA_APP");
        usuario.HasKey(x => x.IdUsuario);

        usuario.Property(x => x.IdUsuario)
            .HasColumnName("ID_USUARIO");

        usuario.Property(x => x.Correo)
            .HasColumnName("CORREO");

        usuario.Property(x => x.Telefono)
            .HasColumnName("TELEFONO");

        usuario.Property(x => x.FechaNacimiento)
            .HasColumnName("FECHA_NACIMIENTO");

        usuario.Property(x => x.Nickname)
            .HasColumnName("NICKNAME");

        usuario.Property(x => x.NotificaEmail)
            .HasColumnName("NOTIFICA_EMAIL");

        usuario.Property(x => x.NotificaWhatsApp)
            .HasColumnName("NOTIFICA_WHATSAPP");
    }

    public override int SaveChanges()
        => throw new NotSupportedException("Oracle central es de solo lectura para NextTech.");

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => throw new NotSupportedException("Oracle central es de solo lectura para NextTech.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Oracle central es de solo lectura para NextTech.");

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Oracle central es de solo lectura para NextTech.");
}
