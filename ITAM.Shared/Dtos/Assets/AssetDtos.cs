using System.ComponentModel.DataAnnotations;
using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Assets;

public class AssetListDto
{
    public Guid Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public AssetKind Kind { get; set; }
    public OwnershipType OwnershipType { get; set; }
    public AssetStatus Status { get; set; }

    public int ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public int? LocationId { get; set; }
    public string? LocationName { get; set; }

    public Guid? CurrentEmployeeId { get; set; }
    public string? CurrentEmployeeName { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? RentalEndDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string? Imei { get; set; }
    public string? ContractNumber { get; set; }
}

public class AssetUpsertDto
{
    [Required, MaxLength(50)]
    public string AssetCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    public AssetKind Kind { get; set; } = AssetKind.Equipment;

    [Range(1, int.MaxValue)]
    public int ModelId { get; set; }

    [Required]
    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;

    public Guid? SupplierId { get; set; }

    [Required]
    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public int? LocationId { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? RentalEndDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }

    [MaxLength(20)]
    public string? Imei { get; set; }

    [MaxLength(80)]
    public string? ContractNumber { get; set; }
}
