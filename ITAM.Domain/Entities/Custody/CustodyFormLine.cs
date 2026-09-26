using ITAM.Domain.Entities.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Entities.Custody;

public class CustodyFormLine : BaseEntity
{
    public Guid CustodyFormId { get; set; }
    public CustodyForm CustodyForm { get; set; } = null!;

    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    public AssetCondition ConditionOnDelivery { get; set; } = AssetCondition.New;
    public string? DeliveryNotes { get; set; }
    public string? ReturnNotes { get; set; }
}
