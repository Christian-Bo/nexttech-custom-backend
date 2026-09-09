using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Producto", "dbo");
        builder.HasKey(x => x.IdProducto);

        builder.Property(x => x.IdProducto)
            .HasColumnName("IdProducto")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdCategoria)
            .HasColumnName("IdCategoria")
            .HasColumnType("int");

        builder.Property(x => x.CodigoProducto)
            .HasColumnName("CodigoProducto")
            .HasColumnType("nvarchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasColumnName("Nombre")
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasColumnName("Descripcion")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000);

        builder.Property(x => x.PrecioBase)
            .HasColumnName("PrecioBase")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.PermitePersonalizacion)
            .HasColumnName("PermitePersonalizacion")
            .HasColumnType("bit");

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
