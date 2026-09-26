using ITAM.Domain.Entities.Assets;
using ITAM.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Assets;

public class AssetMovementConfiguration : IEntityTypeConfiguration<AssetMovement>
{
    public void Configure(EntityTypeBuilder<AssetMovement> builder)
    {
        builder.ToTable("AssetMovements", t =>
            t.HasComment("Historial y trazabilidad de movimientos del activo."));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.PerformedByUserId)
            .HasComment("Usuario Identity que ejecutó el movimiento.");

        builder.HasIndex(x => new { x.AssetId, x.OccurredAt });

        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FromLocation).WithMany().HasForeignKey(x => x.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToLocation).WithMany().HasForeignKey(x => x.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CustodyForm).WithMany().HasForeignKey(x => x.CustodyFormId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
