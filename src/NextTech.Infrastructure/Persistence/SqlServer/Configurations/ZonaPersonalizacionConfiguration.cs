using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ZonaPersonalizacionConfiguration : IEntityTypeConfiguration<ZonaPersonalizacion>
{
    public void Configure(EntityTypeBuilder<ZonaPersonalizacion> builder)
    {
        builder.ToTable("ZonaPersonalizacion", "dbo");
        builder.HasKey(x => x.IdZona);

        builder.Property(x => x.IdZona)
            .HasColumnName("IdZona")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdProducto)
            .HasColumnName("IdProducto")
            .HasColumnType("int");

        builder.Property(x => x.Nombre)
            .HasColumnName("Nombre")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EsObligatoria)
            .HasColumnName("EsObligatoria")
            .HasColumnType("bit");

        builder.Property(x => x.OrdenVisual)
            .HasColumnName("OrdenVisual")
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
