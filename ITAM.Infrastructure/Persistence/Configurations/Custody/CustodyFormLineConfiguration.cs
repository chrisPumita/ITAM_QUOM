using ITAM.Domain.Entities.Custody;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Custody;

public class CustodyFormLineConfiguration : IEntityTypeConfiguration<CustodyFormLine>
{
    public void Configure(EntityTypeBuilder<CustodyFormLine> builder)
    {
        builder.ToTable("CustodyFormLines", t =>
            t.HasComment("Renglones de responsiva: equipo o accesorio."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConditionOnDelivery).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.DeliveryNotes).HasMaxLength(1000);
        builder.Property(x => x.ReturnNotes).HasMaxLength(1000);
        builder.Property(x => x.Quantity).HasDefaultValue(1);

        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
