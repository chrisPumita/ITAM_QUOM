using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Dtos.Company;
using ITAM.Shared.Enums;

namespace ITAM.WebApp.Models.Assets;

public class AssetIndexViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Condition { get; set; }
    public AssetKind? Kind { get; set; }
    public int? BrandId { get; set; }
    public int? ModelId { get; set; }
    public int? CategoryId { get; set; }
    public bool ShowRetired { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public int TotalCount { get; set; }
    public List<AssetListDto> Items { get; set; } = [];
    public List<BrandListDto> Brands { get; set; } = [];
    public List<ModelListDto> Models { get; set; } = [];
    public List<CategoryListDto> Categories { get; set; } = [];
}

public class AssetCreateViewModel
{
    public AssetKind Kind { get; set; } = AssetKind.Equipment;
    public int? FilterBrandId { get; set; }
    public int ModelId { get; set; }
    public string? SerialNumber { get; set; }
    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;
    public Guid? SupplierId { get; set; }
    public int? LocationId { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? RentalEndDate { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string? Imei { get; set; }
    public string? ContractNumber { get; set; }

    public List<BrandListDto> Brands { get; set; } = [];
    public List<ModelListDto> Models { get; set; } = [];
    public List<CategoryListDto> Categories { get; set; } = [];
    public List<LocationListDto> Locations { get; set; } = [];
    public List<SupplierListDto> Suppliers { get; set; } = [];
}

public class AssetEditViewModel : AssetCreateViewModel
{
    public Guid Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public AssetStatus Status { get; set; }
    public AssetCondition Condition { get; set; }
    public bool CanEditCode { get; set; }
}

public class AssetDetailViewModel
{
    public AssetListDto Asset { get; set; } = null!;
    public List<AssetMovementListDto> Movements { get; set; } = [];
    public bool IsAdmin { get; set; }
}

public class AssetImportViewModel
{
    public string? ResultMessage { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> CreatedCodes { get; set; } = [];
    public int Created { get; set; }
    public int Failed { get; set; }
}
