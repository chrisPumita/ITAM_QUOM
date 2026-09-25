using ITAM.Domain.Entities.Company;
using ITAM.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations.Company;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", t =>
            t.HasComment("Colaboradores. IdentityUserId opcional (pueden no tener cuenta)."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeNumber).HasMaxLength(30).IsRequired()
            .HasComment("Número de empleado único.");
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(120);
        builder.HasIndex(x => x.EmployeeNumber).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.IdentityUserId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
