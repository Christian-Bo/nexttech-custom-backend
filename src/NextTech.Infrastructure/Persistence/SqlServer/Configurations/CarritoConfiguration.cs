using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class CarritoConfiguration : IEntityTypeConfiguration<Carrito>
{
    public void Configure(EntityTypeBuilder<Carrito> builder)
    {
        builder.ToTable("Carrito", "dbo");
        builder.HasKey(x => x.IdCarrito);

        builder.Property(x => x.IdCarrito)
            .HasColumnName("IdCarrito")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdCompradorExterno)
            .HasColumnName("IdCompradorExterno")
            .HasColumnType("bigint");

        builder.Property(x => x.IdEstadoCarrito)
            .HasColumnName("IdEstadoCarrito")
            .HasColumnType("int");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.UltimaActividad)
            .HasColumnName("UltimaActividad")
            .HasColumnType("datetime2(0)");
    }
}
