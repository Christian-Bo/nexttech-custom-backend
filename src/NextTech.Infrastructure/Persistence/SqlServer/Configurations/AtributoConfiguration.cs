using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class AtributoConfiguration : IEntityTypeConfiguration<Atributo>
{
    public void Configure(EntityTypeBuilder<Atributo> builder)
    {
        builder.ToTable("Atributo", "dbo");
        builder.HasKey(x => x.IdAtributo);

        builder.Property(x => x.IdAtributo)
            .HasColumnName("IdAtributo")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Nombre)
            .HasColumnName("Nombre")
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
