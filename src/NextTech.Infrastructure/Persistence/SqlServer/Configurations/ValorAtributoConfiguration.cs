using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ValorAtributoConfiguration : IEntityTypeConfiguration<ValorAtributo>
{
    public void Configure(EntityTypeBuilder<ValorAtributo> builder)
    {
        builder.ToTable("ValorAtributo", "dbo");
        builder.HasKey(x => x.IdValorAtributo);

        builder.Property(x => x.IdValorAtributo)
            .HasColumnName("IdValorAtributo")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdAtributo)
            .HasColumnName("IdAtributo")
            .HasColumnType("int");

        builder.Property(x => x.Valor)
            .HasColumnName("Valor")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

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
