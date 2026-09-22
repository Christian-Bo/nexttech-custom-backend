using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class BuyerBiometricCredentialConfiguration : IEntityTypeConfiguration<BuyerBiometricCredentialEntity>
{
    public void Configure(EntityTypeBuilder<BuyerBiometricCredentialEntity> builder)
    {
        builder.ToTable("BiometriaComprador", "dbo");
        builder.HasKey(x => x.IdBiometriaComprador);

        builder.Property(x => x.IdBiometriaComprador)
            .HasColumnName("IdBiometriaComprador")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdCompradorExterno)
            .HasColumnName("IdCompradorExterno")
            .HasColumnType("bigint");

        builder.Property(x => x.BiometricTemplate)
            .HasColumnName("BiometricTemplate")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.TemplateVersion)
            .HasColumnName("TemplateVersion")
            .HasColumnType("nvarchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TemplateKeyId)
            .HasColumnName("TemplateKeyId")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TemplateModel)
            .HasColumnName("TemplateModel")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TemplateDimensions)
            .HasColumnName("TemplateDimensions")
            .HasColumnType("int");

        builder.Property(x => x.EmbeddingSha256)
            .HasColumnName("EmbeddingSha256")
            .HasColumnType("char(64)")
            .HasMaxLength(64);

        builder.Property(x => x.RetratoDatos)
            .HasColumnName("RetratoDatos")
            .HasColumnType("varbinary(max)")
            .IsRequired();

        builder.Property(x => x.RetratoMime)
            .HasColumnName("RetratoMime")
            .HasColumnType("nvarchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RetratoAncho)
            .HasColumnName("RetratoAncho")
            .HasColumnType("int");

        builder.Property(x => x.RetratoAlto)
            .HasColumnName("RetratoAlto")
            .HasColumnType("int");

        builder.Property(x => x.RetratoFondo)
            .HasColumnName("RetratoFondo")
            .HasColumnType("nvarchar(50)")
            .HasMaxLength(50);

        builder.Property(x => x.Activo)
            .HasColumnName("Activo")
            .HasColumnType("bit");

        builder.Property(x => x.FechaEnrolamiento)
            .HasColumnName("FechaEnrolamiento")
            .HasColumnType("datetime2(0)");

        builder.Property(x => x.FechaActualizacion)
            .HasColumnName("FechaActualizacion")
            .HasColumnType("datetime2(0)");

        builder.HasIndex(x => x.IdCompradorExterno)
            .IsUnique()
            .HasDatabaseName("UQ_BiometriaComprador_IdCompradorExterno");
    }
}
