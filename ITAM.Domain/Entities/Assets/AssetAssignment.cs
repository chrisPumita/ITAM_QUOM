using ITAM.Domain.Entities.Company;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Entities.Assets;

/// <summary>
/// Asignación de un activo a un colaborador. Solo una activa por AssetId (ReturnedAt null).
/// Guarda quién ejecutó la operación (Identity).
/// </summary>
public class AssetAssignment : BaseEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }

    public AssetCondition? ReturnCondition { get; set; }
    public string? Notes { get; set; }

    /// <summary>Usuario Identity que ejecutó la asignación.</summary>
    public Guid AssignedByUserId { get; set; }

    /// <summary>Usuario Identity que ejecutó la devolución.</summary>
    public Guid? ReturnedByUserId { get; set; }
}
