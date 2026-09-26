using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Entities.Company;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Entities.Assets;

/// <summary>
/// Historial / auditoría de un activo. Incluye siempre quién ejecutó la acción.
/// </summary>
public class AssetMovement : BaseEntity
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public MovementType MovementType { get; set; }

    public AssetStatus? FromStatus { get; set; }
    public AssetStatus? ToStatus { get; set; }

    public int? FromLocationId { get; set; }
    public Location? FromLocation { get; set; }

    public int? ToLocationId { get; set; }
    public Location? ToLocation { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Usuario Identity que ejecutó el movimiento (trazabilidad operativa).</summary>
    public Guid PerformedByUserId { get; set; }

    public Guid? CustodyFormId { get; set; }
    public Custody.CustodyForm? CustodyForm { get; set; }

    public string? Notes { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
