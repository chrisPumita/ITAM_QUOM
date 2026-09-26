using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Company;

public class EmployeeListDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public Guid? IdentityUserId { get; set; }
}

public class EmployeeUpsertDto
{
    [Required, MaxLength(30)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Opcional: vincula a AspNetUsers. Null = sin cuenta.</summary>
    public Guid? IdentityUserId { get; set; }
}
