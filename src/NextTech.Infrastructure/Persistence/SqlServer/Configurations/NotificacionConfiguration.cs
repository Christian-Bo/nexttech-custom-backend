using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificacion", "dbo");
        builder.HasKey(x => x.IdNotificacion);

        builder.Property(x => x.IdNotificacion)
            .HasColumnName("IdNotificacion")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdOrden)
            .HasColumnName("IdOrden")
            .HasColumnType("int");

        builder.Property(x => x.IdTipoNotificacion)
            .HasColumnName("IdTipoNotificacion")
            .HasColumnType("int");

        builder.Property(x => x.IdCanalNotificacion)
            .HasColumnName("IdCanalNotificacion")
            .HasColumnType("int");

        builder.Property(x => x.IdEstadoNotificacion)
            .HasColumnName("IdEstadoNotificacion")
            .HasColumnType("int");

        builder.Property(x => x.Destino)
            .HasColumnName("Destino")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaEnvio)
            .HasColumnName("FechaEnvio")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.Intentos)
            .HasColumnName("Intentos")
            .HasColumnType("int");

        builder.Property(x => x.MensajeError)
            .HasColumnName("MensajeError")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000);
    }
}
