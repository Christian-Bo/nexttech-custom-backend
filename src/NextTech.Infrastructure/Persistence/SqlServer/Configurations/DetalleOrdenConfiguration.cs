using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class DetalleOrdenConfiguration : IEntityTypeConfiguration<DetalleOrden>
{
    public void Configure(EntityTypeBuilder<DetalleOrden> builder)
    {
        builder.ToTable("DetalleOrden", "dbo");
        builder.HasKey(x => x.IdDetalleOrden);

        builder.Property(x => x.IdDetalleOrden)
            .HasColumnName("IdDetalleOrden")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdOrden)
            .HasColumnName("IdOrden")
            .HasColumnType("int");

        builder.Property(x => x.IdVariante)
            .HasColumnName("IdVariante")
            .HasColumnType("int");

        builder.Property(x => x.IdPersonalizacion)
            .HasColumnName("IdPersonalizacion")
            .HasColumnType("int");

        builder.Property(x => x.Cantidad)
            .HasColumnName("Cantidad")
            .HasColumnType("int");

        builder.Property(x => x.NombreProductoAplicado)
            .HasColumnName("NombreProductoAplicado")
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.NombreVarianteAplicada)
            .HasColumnName("NombreVarianteAplicada")
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.AtributosAplicadosJson)
            .HasColumnName("AtributosAplicadosJson")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.PrecioBaseAplicado)
            .HasColumnName("PrecioBaseAplicado")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.PrecioVarianteAplicado)
            .HasColumnName("PrecioVarianteAplicado")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.PrecioUnitario)
            .HasColumnName("PrecioUnitario")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Subtotal)
            .HasColumnName("Subtotal")
            .HasColumnType("decimal(10,2)");
    }
}
