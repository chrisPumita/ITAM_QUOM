using ITAM.Domain.Entities.Company;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Entities.Custody;

public class CustodyForm : BaseEntity
{
    public string Folio { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public CustodyFormStatus Status { get; set; } = CustodyFormStatus.Draft;

    public DateTime? IssuedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Notes { get; set; }

    public Guid IssuedByUserId { get; set; }

    public ICollection<CustodyFormLine> Lines { get; set; } = new List<CustodyFormLine>();
}
