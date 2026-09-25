using ITAM.Domain.Entities.Assets;
using ITAM.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Assets;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", t =>
            t.HasComment("Activos TI: equipos y accesorios de inventario."));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetCode).HasMaxLength(50).IsRequired()
            .HasComment("Código de inventario / etiqueta.");
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.Imei).HasMaxLength(20);
        builder.Property(x => x.ContractNumber).HasMaxLength(80);

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.OwnershipType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.AssetCode).IsUnique();
        builder.HasIndex(x => x.SerialNumber).IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL");
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Kind);

        builder.HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CurrentEmployee).WithMany().HasForeignKey(x => x.CurrentEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

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
