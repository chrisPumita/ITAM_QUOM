using ITAM.Domain.Entities.Company;
using ITAM.Domain.Enums;

namespace ITAM.Domain.Entities.Custody;

/// <summary>
/// Responsiva (documento de resguardo). Folio RES-yyyy-####. Puede incluir varios assets/accesorios.
/// </summary>
public class CustodyForm : BaseEntity
{
    /// <summary>Folio único generado con FolioCounter (ej. RES-2026-0001).</summary>
    public string Folio { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public CustodyFormStatus Status { get; set; } = CustodyFormStatus.Draft;

    public DateTime? IssuedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Usuario que emitió / generó la responsiva.</summary>
    public Guid IssuedByUserId { get; set; }

    public ICollection<CustodyFormLine> Lines { get; set; } = new List<CustodyFormLine>();
}
