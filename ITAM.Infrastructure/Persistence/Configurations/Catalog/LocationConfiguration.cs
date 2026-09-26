using ITAM.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Catalog;

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
