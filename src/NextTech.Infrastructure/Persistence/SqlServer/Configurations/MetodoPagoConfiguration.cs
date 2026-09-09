using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class MetodoPagoConfiguration : IEntityTypeConfiguration<MetodoPago>
{
    public void Configure(EntityTypeBuilder<MetodoPago> builder)
    {
        builder.ToTable("MetodoPago", "dbo");
        builder.HasKey(x => x.IdMetodoPago);

        builder.Property(x => x.IdMetodoPago)
            .HasColumnName("IdMetodoPago")
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
