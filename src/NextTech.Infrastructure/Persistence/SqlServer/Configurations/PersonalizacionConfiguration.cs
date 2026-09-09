using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class PersonalizacionConfiguration : IEntityTypeConfiguration<Personalizacion>
{
    public void Configure(EntityTypeBuilder<Personalizacion> builder)
    {
        builder.ToTable("Personalizacion", "dbo");
        builder.HasKey(x => x.IdPersonalizacion);

        builder.Property(x => x.IdPersonalizacion)
            .HasColumnName("IdPersonalizacion")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdVariante)
            .HasColumnName("IdVariante")
            .HasColumnType("int");

        builder.Property(x => x.Bloqueada)
            .HasColumnName("Bloqueada")
            .HasColumnType("bit");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");
    }
}
