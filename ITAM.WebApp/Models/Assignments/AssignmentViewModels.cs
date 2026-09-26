using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Dtos.Company;
using ITAM.Shared.Enums;

namespace ITAM.WebApp.Models.Assignments;

public class AssignViewModel
{
    public Guid? EmployeeId { get; set; }
    public string? Notes { get; set; }
    public List<Guid> SelectedAssetIds { get; set; } = [];
    public List<EmployeeListDto> Employees { get; set; } = [];
    public List<AssetListDto> AvailableAssets { get; set; } = [];
    public List<BrandListDto> Brands { get; set; } = [];
    public List<ModelListDto> Models { get; set; } = [];
    public string? Search { get; set; }
    public string? PrefillAssetCode { get; set; }
    public AssetKind? Kind { get; set; }
    public AssetCondition? Condition { get; set; }
    public int? BrandId { get; set; }
    public int? ModelId { get; set; }
}

public class ReturnViewModel
{
    public string Mode { get; set; } = "folio"; // folio | code
    public string? Folio { get; set; }
    public string? AssetCode { get; set; }
    public string? Notes { get; set; }
    public List<Guid> SelectedAssetIds { get; set; } = [];
    public CustodyFormDetailDto? Custody { get; set; }
    public AssetListDto? LookupAsset { get; set; }
    public List<AssignmentListDto> ActiveLines { get; set; } = [];
}

public class ResponsivasViewModel
{
    public string? Folio { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public List<EmployeeListDto> Employees { get; set; } = [];
    public List<CustodyFormListDto> Items { get; set; } = [];
}
