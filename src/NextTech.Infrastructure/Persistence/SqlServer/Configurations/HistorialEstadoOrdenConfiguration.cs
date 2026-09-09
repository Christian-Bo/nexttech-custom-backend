using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class HistorialEstadoOrdenConfiguration : IEntityTypeConfiguration<HistorialEstadoOrden>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoOrden> builder)
    {
        builder.ToTable("HistorialEstadoOrden", "dbo");
        builder.HasKey(x => x.IdHistorialEstado);

        builder.Property(x => x.IdHistorialEstado)
            .HasColumnName("IdHistorialEstado")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdOrden)
            .HasColumnName("IdOrden")
            .HasColumnType("int");

        builder.Property(x => x.IdEstadoOrden)
            .HasColumnName("IdEstadoOrden")
            .HasColumnType("int");

        builder.Property(x => x.IdTipoActor)
            .HasColumnName("IdTipoActor")
            .HasColumnType("int");

        builder.Property(x => x.IdUsuarioInterno)
            .HasColumnName("IdUsuarioInterno")
            .HasColumnType("int");

        builder.Property(x => x.IdCompradorExterno)
            .HasColumnName("IdCompradorExterno")
            .HasColumnType("bigint");

        builder.Property(x => x.FechaHora)
            .HasColumnName("FechaHora")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.Observacion)
            .HasColumnName("Observacion")
            .HasColumnType("nvarchar(500)")
            .HasMaxLength(500);
    }
}
