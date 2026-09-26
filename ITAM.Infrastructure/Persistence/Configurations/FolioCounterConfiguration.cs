using ITAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations;

public class FolioCounterConfiguration : IEntityTypeConfiguration<FolioCounter>
{
    public void Configure(EntityTypeBuilder<FolioCounter> builder)
    {
        builder.ToTable("FolioCounters", t =>
            t.HasComment("Contadores de folio (responsivas y documentos)."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired()
            .HasComment("Prefijo del documento, ej. RES.");
        builder.HasIndex(x => new { x.Prefix, x.Year }).IsUnique();
    }
}
