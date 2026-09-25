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

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands", t => t.HasComment("Catálogo de marcas."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

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

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations", t => t.HasComment("Ubicaciones físicas / bodegas."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasOne(x => x.ParentLocation).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentLocationId).OnDelete(DeleteBehavior.Restrict);
    }
}
