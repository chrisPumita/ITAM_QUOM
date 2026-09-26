using ITAM.Domain.Entities.Company;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Entities.Assets;

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

    public Guid AssignedByUserId { get; set; }

    public Guid? ReturnedByUserId { get; set; }
}
