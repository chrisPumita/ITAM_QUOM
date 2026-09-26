using System.ComponentModel.DataAnnotations;
using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Assets;

public class AssetImportRowDto
{
    public int RowNumber { get; set; }
    public AssetKind Kind { get; set; } = AssetKind.Equipment;

    public string BrandName { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string? CategoryName { get; set; }

    public string? Specs { get; set; }
    public string? SerialNumber { get; set; }
    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;
    public string? SupplierName { get; set; }
    public string? LocationName { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string? Imei { get; set; }
    public string? ContractNumber { get; set; }

    // Resuelto en servidor al importar
    public int ModelId { get; set; }
    public Guid? SupplierId { get; set; }
    public int? LocationId { get; set; }
}

public class AssetImportRequestDto
{
    [Required]
    public List<AssetImportRowDto> Rows { get; set; } = [];
}

public class AssetImportResultDto
{
    public int Created { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> CreatedCodes { get; set; } = [];
}
