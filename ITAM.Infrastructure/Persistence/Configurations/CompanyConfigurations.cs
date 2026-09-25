using ITAM.Domain.Entities;
using ITAM.Domain.Entities.Company;
using ITAM.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAM.Infrastructure.Persistence.Configurations;

public class FolioCounterConfiguration : IEntityTypeConfiguration<FolioCounter>
{
    public void Configure(EntityTypeBuilder<FolioCounter> builder)
    {
        builder.ToTable("FolioCounters", t =>
            t.HasComment("Contadores de folio (responsivas y documentos)."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired()
            .HasComment("Prefijo del documento, ej. RES.");
        builder.HasIndex(x => new { x.Prefix, x.Year }).IsUnique();
    }
}

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

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", t =>
            t.HasComment("Proveedores: compra, mantenimiento y/o arrendamiento."));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Contact).HasMaxLength(150);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
