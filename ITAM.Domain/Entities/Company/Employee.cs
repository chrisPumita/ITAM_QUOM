namespace ITAM.Domain.Entities.Company;

public class Employee : BaseEntity
{
    public string EmployeeNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? IdentityUserId { get; set; }
}
