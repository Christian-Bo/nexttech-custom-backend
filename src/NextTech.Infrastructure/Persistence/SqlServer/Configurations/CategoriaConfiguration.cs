using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categoria", "dbo");
        builder.HasKey(x => x.IdCategoria);

        builder.Property(x => x.IdCategoria)
            .HasColumnName("IdCategoria")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Nombre)
            .HasColumnName("Nombre")
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasColumnName("Descripcion")
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500);

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
