using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Configurations;

public sealed class TipoActorConfiguration : IEntityTypeConfiguration<TipoActor>
{
    public void Configure(EntityTypeBuilder<TipoActor> builder)
    {
        builder.ToTable("TipoActor", "dbo");
        builder.HasKey(x => x.IdTipoActor);

        builder.Property(x => x.IdTipoActor)
            .HasColumnName("IdTipoActor")
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
