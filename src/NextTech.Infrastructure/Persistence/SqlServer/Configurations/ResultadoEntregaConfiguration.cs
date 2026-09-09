using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class ResultadoEntregaConfiguration : IEntityTypeConfiguration<ResultadoEntrega>
{
    public void Configure(EntityTypeBuilder<ResultadoEntrega> builder)
    {
        builder.ToTable("ResultadoEntrega", "dbo");
        builder.HasKey(x => x.IdResultadoEntrega);

        builder.Property(x => x.IdResultadoEntrega)
            .HasColumnName("IdResultadoEntrega")
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
