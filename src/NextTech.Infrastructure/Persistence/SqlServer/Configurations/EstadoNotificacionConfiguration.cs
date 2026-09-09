using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class EstadoNotificacionConfiguration : IEntityTypeConfiguration<EstadoNotificacion>
{
    public void Configure(EntityTypeBuilder<EstadoNotificacion> builder)
    {
        builder.ToTable("EstadoNotificacion", "dbo");
        builder.HasKey(x => x.IdEstadoNotificacion);

        builder.Property(x => x.IdEstadoNotificacion)
            .HasColumnName("IdEstadoNotificacion")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Codigo)
            .HasColumnName("Codigo")
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasColumnName("Nombre")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();
    }
}
