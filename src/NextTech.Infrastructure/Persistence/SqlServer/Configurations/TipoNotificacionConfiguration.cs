using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class TipoNotificacionConfiguration : IEntityTypeConfiguration<TipoNotificacion>
{
    public void Configure(EntityTypeBuilder<TipoNotificacion> builder)
    {
        builder.ToTable("TipoNotificacion", "dbo");
        builder.HasKey(x => x.IdTipoNotificacion);

        builder.Property(x => x.IdTipoNotificacion)
            .HasColumnName("IdTipoNotificacion")
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
