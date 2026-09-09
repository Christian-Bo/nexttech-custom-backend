using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class VarianteAtributoConfiguration : IEntityTypeConfiguration<VarianteAtributo>
{
    public void Configure(EntityTypeBuilder<VarianteAtributo> builder)
    {
        builder.ToTable("VarianteAtributo", "dbo");
        builder.HasKey(x => x.IdVarianteAtributo);

        builder.Property(x => x.IdVarianteAtributo)
            .HasColumnName("IdVarianteAtributo")
            .HasColumnType("int")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdVariante)
            .HasColumnName("IdVariante")
            .HasColumnType("int");

        builder.Property(x => x.IdAtributo)
            .HasColumnName("IdAtributo")
            .HasColumnType("int");

        builder.Property(x => x.IdValorAtributo)
            .HasColumnName("IdValorAtributo")
            .HasColumnType("int");
    }
}
