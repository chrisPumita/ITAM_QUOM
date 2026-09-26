namespace ITAM.Domain.Entities.Company;

/// <summary>Colaborador. Puede existir sin cuenta de login.</summary>
public class Employee : BaseEntity
{
    /// <summary>Número de empleado único (nómina / RH).</summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Correo de contacto único; no implica acceso al sistema.</summary>
    public string Email { get; set; } = string.Empty;

    public string? Department { get; set; }

    /// <summary>Inactivo no puede recibir activos.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Usuario Identity vinculado (AspNetUsers). Null = sin cuenta.
    /// </summary>
    public Guid? IdentityUserId { get; set; }
}
