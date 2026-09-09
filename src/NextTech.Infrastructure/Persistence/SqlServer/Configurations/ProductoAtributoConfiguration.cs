using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ProductoAtributoConfiguration : IEntityTypeConfiguration<ProductoAtributo>
{
    public void Configure(EntityTypeBuilder<ProductoAtributo> builder)
    {
        builder.ToTable("ProductoAtributo", "dbo");
        builder.HasKey(x => x.IdProductoAtributo);

        builder.Property(x => x.IdProductoAtributo)
            .HasColumnName("IdProductoAtributo")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdProducto)
            .HasColumnName("IdProducto")
            .HasColumnType("int");

        builder.Property(x => x.IdAtributo)
            .HasColumnName("IdAtributo")
            .HasColumnType("int");

        builder.Property(x => x.EsObligatorio)
            .HasColumnName("EsObligatorio")
            .HasColumnType("bit");

        builder.Property(x => x.OrdenVisual)
            .HasColumnName("OrdenVisual")
            .HasColumnType("int");
    }
}
