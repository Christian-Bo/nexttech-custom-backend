using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class BitacoraAuditoriaConfiguration : IEntityTypeConfiguration<BitacoraAuditoria>
{
    public void Configure(EntityTypeBuilder<BitacoraAuditoria> builder)
    {
        builder.ToTable("BitacoraAuditoria", "dbo");
        builder.HasKey(x => x.IdBitacora);

        builder.Property(x => x.IdBitacora)
            .HasColumnName("IdBitacora")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdTipoActor)
            .HasColumnName("IdTipoActor")
            .HasColumnType("int");

        builder.Property(x => x.IdUsuarioInterno)
            .HasColumnName("IdUsuarioInterno")
            .HasColumnType("int");

        builder.Property(x => x.IdCompradorExterno)
            .HasColumnName("IdCompradorExterno")
            .HasColumnType("bigint");

        builder.Property(x => x.Accion)
            .HasColumnName("Accion")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Entidad)
            .HasColumnName("Entidad")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IdEntidad)
            .HasColumnName("IdEntidad")
            .HasColumnType("int");

        builder.Property(x => x.Resultado)
            .HasColumnName("Resultado")
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DireccionIp)
            .HasColumnName("DireccionIp")
            .HasColumnType("nvarchar(45)")
            .HasMaxLength(45);

        builder.Property(x => x.Detalle)
            .HasColumnName("Detalle")
            .HasColumnType("nvarchar(1000)")
            .HasMaxLength(1000);

        builder.Property(x => x.FechaHora)
            .HasColumnName("FechaHora")
            .HasColumnType("datetime2(0)");
    }
}
