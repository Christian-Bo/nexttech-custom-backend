using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class PersonalizacionZonaConfiguration : IEntityTypeConfiguration<PersonalizacionZona>
{
    public void Configure(EntityTypeBuilder<PersonalizacionZona> builder)
    {
        builder.ToTable("PersonalizacionZona", "dbo");
        builder.HasKey(x => x.IdPersonalizacionZona);

        builder.Property(x => x.IdPersonalizacionZona)
            .HasColumnName("IdPersonalizacionZona")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdPersonalizacion)
            .HasColumnName("IdPersonalizacion")
            .HasColumnType("int");

        builder.Property(x => x.IdZona)
            .HasColumnName("IdZona")
            .HasColumnType("int");

        builder.Property(x => x.IdArchivoImagenFinal)
            .HasColumnName("IdArchivoImagenFinal")
            .HasColumnType("int");

        builder.Property(x => x.ConfiguracionJson)
            .HasColumnName("ConfiguracionJson")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");
    }
}
