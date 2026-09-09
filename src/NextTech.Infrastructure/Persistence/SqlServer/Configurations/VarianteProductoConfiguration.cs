using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class VarianteProductoConfiguration : IEntityTypeConfiguration<VarianteProducto>
{
    public void Configure(EntityTypeBuilder<VarianteProducto> builder)
    {
        builder.ToTable("VarianteProducto", "dbo");
        builder.HasKey(x => x.IdVariante);

        builder.Property(x => x.IdVariante)
            .HasColumnName("IdVariante")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdProducto)
            .HasColumnName("IdProducto")
            .HasColumnType("int");

        builder.Property(x => x.CodigoVariante)
            .HasColumnName("CodigoVariante")
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
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500);

        builder.Property(x => x.PrecioAdicional)
            .HasColumnName("PrecioAdicional")
            .HasColumnType("decimal(10,2)");

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
