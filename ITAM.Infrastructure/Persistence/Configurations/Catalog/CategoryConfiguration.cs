using ITAM.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Catalog;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t => t.HasComment("Catálogo de categorías de activos TI."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired().HasComment("Nombre visible de la categoría.");
        builder.Property(x => x.SortOrder).HasComment("Orden de visualización.");
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasOne(x => x.ParentCategory).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
