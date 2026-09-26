using ITAM.Domain.Entities.Assets;
using ITAM.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Assets;

public class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("AssetAssignments", t =>
            t.HasComment("Asignaciones activo-colaborador. Una activa por activo."));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnCondition).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.AssignedByUserId)
            .HasComment("Usuario Identity que ejecutó la asignación.");

        builder.HasIndex(x => x.AssetId).IsUnique()
            .HasFilter("[ReturnedAt] IS NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_AssetAssignments_ActiveAsset");

        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReturnedByUserId)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
