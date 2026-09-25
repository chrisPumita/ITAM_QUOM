using ITAM.Domain.Entities.Assets;
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
