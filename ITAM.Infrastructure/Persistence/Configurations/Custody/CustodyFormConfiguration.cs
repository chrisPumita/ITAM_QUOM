using ITAM.Domain.Entities.Custody;
using ITAM.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Custody;

public class CustodyFormConfiguration : IEntityTypeConfiguration<CustodyForm>
{
    public void Configure(EntityTypeBuilder<CustodyForm> builder)
    {
        builder.ToTable("CustodyForms", t =>
            t.HasComment("Responsivas (resguardo). Folio RES-yyyy-####. Multi-renglón."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Folio).HasMaxLength(40).IsRequired()
            .HasComment("Folio único de responsiva.");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => x.Folio).IsUnique();

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.CustodyForm)
            .HasForeignKey(x => x.CustodyFormId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
