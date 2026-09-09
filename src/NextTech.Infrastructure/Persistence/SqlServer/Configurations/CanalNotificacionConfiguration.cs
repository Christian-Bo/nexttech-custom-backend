using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class CanalNotificacionConfiguration : IEntityTypeConfiguration<CanalNotificacion>
{
    public void Configure(EntityTypeBuilder<CanalNotificacion> builder)
    {
        builder.ToTable("CanalNotificacion", "dbo");
        builder.HasKey(x => x.IdCanalNotificacion);

        builder.Property(x => x.IdCanalNotificacion)
            .HasColumnName("IdCanalNotificacion")
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
