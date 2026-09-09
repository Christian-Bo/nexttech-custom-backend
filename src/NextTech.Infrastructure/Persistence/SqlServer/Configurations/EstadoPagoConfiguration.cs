using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class EstadoPagoConfiguration : IEntityTypeConfiguration<EstadoPago>
{
    public void Configure(EntityTypeBuilder<EstadoPago> builder)
    {
        builder.ToTable("EstadoPago", "dbo");
        builder.HasKey(x => x.IdEstadoPago);

        builder.Property(x => x.IdEstadoPago)
            .HasColumnName("IdEstadoPago")
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
