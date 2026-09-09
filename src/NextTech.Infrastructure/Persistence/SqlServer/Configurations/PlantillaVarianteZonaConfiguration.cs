using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class PlantillaVarianteZonaConfiguration : IEntityTypeConfiguration<PlantillaVarianteZona>
{
    public void Configure(EntityTypeBuilder<PlantillaVarianteZona> builder)
    {
        builder.ToTable("PlantillaVarianteZona", "dbo");
        builder.HasKey(x => x.IdPlantilla);

        builder.Property(x => x.IdPlantilla)
            .HasColumnName("IdPlantilla")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdVariante)
            .HasColumnName("IdVariante")
            .HasColumnType("int");

        builder.Property(x => x.IdZona)
            .HasColumnName("IdZona")
            .HasColumnType("int");

        builder.Property(x => x.Forma)
            .HasColumnName("Forma")
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AnchoLienzo)
            .HasColumnName("AnchoLienzo")
            .HasColumnType("int");

        builder.Property(x => x.AltoLienzo)
            .HasColumnName("AltoLienzo")
            .HasColumnType("int");

        builder.Property(x => x.IdArchivoMascara)
            .HasColumnName("IdArchivoMascara")
            .HasColumnType("int");

        builder.Property(x => x.Activo)
            .HasColumnName("Activo")
            .HasColumnType("bit");

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
