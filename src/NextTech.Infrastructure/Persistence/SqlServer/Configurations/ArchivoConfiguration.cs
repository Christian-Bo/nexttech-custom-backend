using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ArchivoConfiguration : IEntityTypeConfiguration<Archivo>
{
    public void Configure(EntityTypeBuilder<Archivo> builder)
    {
        builder.ToTable("Archivo", "dbo");
        builder.HasKey(x => x.IdArchivo);

        builder.Property(x => x.IdArchivo)
            .HasColumnName("IdArchivo")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.NombreOriginal)
            .HasColumnName("NombreOriginal")
            .HasColumnType("nvarchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.TipoMime)
            .HasColumnName("TipoMime")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Extension)
            .HasColumnName("Extension")
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Datos)
            .HasColumnName("Datos")
            .HasColumnType("varbinary(max)")
            .IsRequired();

        builder.Property(x => x.TamanoBytes)
            .HasColumnName("TamanoBytes")
            .HasColumnType("bigint");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");
    }
}
