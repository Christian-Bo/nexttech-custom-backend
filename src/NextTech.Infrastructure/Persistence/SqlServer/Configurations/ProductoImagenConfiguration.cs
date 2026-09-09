using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ProductoImagenConfiguration : IEntityTypeConfiguration<ProductoImagen>
{
    public void Configure(EntityTypeBuilder<ProductoImagen> builder)
    {
        builder.ToTable("ProductoImagen", "dbo");
        builder.HasKey(x => x.IdProductoImagen);

        builder.Property(x => x.IdProductoImagen)
            .HasColumnName("IdProductoImagen")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdProducto)
            .HasColumnName("IdProducto")
            .HasColumnType("int");

        builder.Property(x => x.IdArchivo)
            .HasColumnName("IdArchivo")
            .HasColumnType("int");

        builder.Property(x => x.EsPrincipal)
            .HasColumnName("EsPrincipal")
            .HasColumnType("bit");

        builder.Property(x => x.OrdenVisual)
            .HasColumnName("OrdenVisual")
            .HasColumnType("int");
    }
}
