using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class OrdenConfiguration : IEntityTypeConfiguration<Orden>
{
    public void Configure(EntityTypeBuilder<Orden> builder)
    {
        builder.ToTable("Orden", "dbo");
        builder.HasKey(x => x.IdOrden);

        builder.Property(x => x.IdOrden)
            .HasColumnName("IdOrden")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CodigoOrden)
            .HasColumnName("CodigoOrden")
            .HasColumnType("nvarchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.IdCompradorExterno)
            .HasColumnName("IdCompradorExterno")
            .HasColumnType("bigint");

        builder.Property(x => x.NicknameCompradorAplicado)
            .HasColumnName("NicknameCompradorAplicado")
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CorreoCompradorAplicado)
            .HasColumnName("CorreoCompradorAplicado")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TelefonoCompradorAplicado)
            .HasColumnName("TelefonoCompradorAplicado")
            .HasColumnType("nvarchar(30)")
            .HasMaxLength(30);

        builder.Property(x => x.IdCarritoOrigen)
            .HasColumnName("IdCarritoOrigen")
            .HasColumnType("int");

        builder.Property(x => x.IdAreaEntrega)
            .HasColumnName("IdAreaEntrega")
            .HasColumnType("int");

        builder.Property(x => x.NombreAreaAplicado)
            .HasColumnName("NombreAreaAplicado")
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ReferenciaEntrega)
            .HasColumnName("ReferenciaEntrega")
            .HasColumnType("nvarchar(300)")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.IdEstadoOrdenActual)
            .HasColumnName("IdEstadoOrdenActual")
            .HasColumnType("int");

        builder.Property(x => x.IdRepartidorAsignado)
            .HasColumnName("IdRepartidorAsignado")
            .HasColumnType("int");

        builder.Property(x => x.Total)
            .HasColumnName("Total")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.IdArchivoConstancia)
            .HasColumnName("IdArchivoConstancia")
            .HasColumnType("int");

        builder.Property(x => x.QrUtilizado)
            .HasColumnName("QrUtilizado")
            .HasColumnType("bit");

        builder.Property(x => x.FechaUsoQr)
            .HasColumnName("FechaUsoQr")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("FechaCreacion")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");
    }
}
