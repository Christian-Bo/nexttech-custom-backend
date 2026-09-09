using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class DetalleCarritoConfiguration : IEntityTypeConfiguration<DetalleCarrito>
{
    public void Configure(EntityTypeBuilder<DetalleCarrito> builder)
    {
        builder.ToTable("DetalleCarrito", "dbo");
        builder.HasKey(x => x.IdDetalleCarrito);

        builder.Property(x => x.IdDetalleCarrito)
            .HasColumnName("IdDetalleCarrito")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdCarrito)
            .HasColumnName("IdCarrito")
            .HasColumnType("int");

        builder.Property(x => x.IdVariante)
            .HasColumnName("IdVariante")
            .HasColumnType("int");

        builder.Property(x => x.Cantidad)
            .HasColumnName("Cantidad")
            .HasColumnType("int");

        builder.Property(x => x.IdPersonalizacion)
            .HasColumnName("IdPersonalizacion")
            .HasColumnType("int");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");
    }
}
