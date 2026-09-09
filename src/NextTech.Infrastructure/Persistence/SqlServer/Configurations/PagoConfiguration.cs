using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pago", "dbo");
        builder.HasKey(x => x.IdPago);

        builder.Property(x => x.IdPago)
            .HasColumnName("IdPago")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdOrden)
            .HasColumnName("IdOrden")
            .HasColumnType("int");

        builder.Property(x => x.IdMetodoPago)
            .HasColumnName("IdMetodoPago")
            .HasColumnType("int");

        builder.Property(x => x.IdEstadoPago)
            .HasColumnName("IdEstadoPago")
            .HasColumnType("int");

        builder.Property(x => x.Monto)
            .HasColumnName("Monto")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.ReferenciaTransaccion)
            .HasColumnName("ReferenciaTransaccion")
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150);

        builder.Property(x => x.MarcaTarjeta)
            .HasColumnName("MarcaTarjeta")
            .HasColumnType("nvarchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.Ultimos4)
            .HasColumnName("Ultimos4")
            .HasColumnType("nvarchar(4)")
            .HasMaxLength(4);

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaPago)
            .HasColumnName("FechaPago")
            .HasColumnType("datetime2(0)");
    }
}
