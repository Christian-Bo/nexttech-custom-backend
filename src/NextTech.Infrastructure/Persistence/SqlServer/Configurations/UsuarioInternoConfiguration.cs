using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class UsuarioInternoConfiguration : IEntityTypeConfiguration<UsuarioInterno>
{
    public void Configure(EntityTypeBuilder<UsuarioInterno> builder)
    {
        builder.ToTable("UsuarioInterno", "dbo");
        builder.HasKey(x => x.IdUsuarioInterno);

        builder.Property(x => x.IdUsuarioInterno)
            .HasColumnName("IdUsuarioInterno")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdRol)
            .HasColumnName("IdRol")
            .HasColumnType("int");

        builder.Property(x => x.Nombres)
            .HasColumnName("Nombres")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Apellidos)
            .HasColumnName("Apellidos")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Correo)
            .HasColumnName("Correo")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Activo)
            .HasColumnName("Activo")
            .HasColumnType("bit");

        builder.Property(x => x.DebeCambiarPassword)
            .HasColumnName("DebeCambiarPassword")
            .HasColumnType("bit");

        builder.Property(x => x.IntentosFallidos)
            .HasColumnName("IntentosFallidos")
            .HasColumnType("int");

        builder.Property(x => x.BloqueadoHasta)
            .HasColumnName("BloqueadoHasta")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.UltimoAcceso)
            .HasColumnName("UltimoAcceso")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaDesactivacion)
            .HasColumnName("FechaDesactivacion")
            .HasColumnType("datetime2(0)");
    }
}
