using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class IntentoEntregaConfiguration : IEntityTypeConfiguration<IntentoEntrega>
{
    public void Configure(EntityTypeBuilder<IntentoEntrega> builder)
    {
        builder.ToTable("IntentoEntrega", "dbo");
        builder.HasKey(x => x.IdIntentoEntrega);

        builder.Property(x => x.IdIntentoEntrega)
            .HasColumnName("IdIntentoEntrega")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdOrden)
            .HasColumnName("IdOrden")
            .HasColumnType("int");

        builder.Property(x => x.IdRepartidor)
            .HasColumnName("IdRepartidor")
            .HasColumnType("int");

        builder.Property(x => x.FechaHoraInicio)
            .HasColumnName("FechaHoraInicio")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaHoraFin)
            .HasColumnName("FechaHoraFin")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.IdResultadoEntrega)
            .HasColumnName("IdResultadoEntrega")
            .HasColumnType("int");

        builder.Property(x => x.Observacion)
            .HasColumnName("Observacion")
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500);

        builder.Property(x => x.IdArchivoFotoEntrega)
            .HasColumnName("IdArchivoFotoEntrega")
            .HasColumnType("int");
    }
}
