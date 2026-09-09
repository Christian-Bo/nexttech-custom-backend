using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class EstadoCarritoConfiguration : IEntityTypeConfiguration<EstadoCarrito>
{
    public void Configure(EntityTypeBuilder<EstadoCarrito> builder)
    {
        builder.ToTable("EstadoCarrito", "dbo");
        builder.HasKey(x => x.IdEstadoCarrito);

        builder.Property(x => x.IdEstadoCarrito)
            .HasColumnName("IdEstadoCarrito")
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
