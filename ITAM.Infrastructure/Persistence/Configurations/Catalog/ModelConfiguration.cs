using ITAM.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Catalog;

public class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("Models", t => t.HasComment("Modelos comerciales (categoría + marca)."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Specs).HasMaxLength(2000);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CategoryId, x.BrandId, x.Name }).IsUnique();
    }
}
