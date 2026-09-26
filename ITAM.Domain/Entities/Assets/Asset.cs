using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Entities.Company;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Entities.Assets;

public class Asset : BaseEntity
{
    public string AssetCode { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public AssetKind Kind { get; set; } = AssetKind.Equipment;

    public int ModelId { get; set; }
    public Model Model { get; set; } = null!;

    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public AssetCondition Condition { get; set; } = AssetCondition.New;

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? RentalEndDate { get; set; }
    public string? Imei { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string? ContractNumber { get; set; }

    public Guid? CurrentEmployeeId { get; set; }
    public Employee? CurrentEmployee { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
