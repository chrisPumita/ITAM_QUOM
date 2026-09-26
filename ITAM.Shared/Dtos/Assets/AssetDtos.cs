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
    public AssetCondition Condition { get; set; }

    /// <summary>Etiqueta UI: Nuevo / Disponible / Asignado / …</summary>
    public string StatusLabel => Status switch
    {
        AssetStatus.Available when Condition == AssetCondition.New => "Nuevo",
        AssetStatus.Available => "Disponible",
        AssetStatus.Assigned => "Asignado",
        AssetStatus.Maintenance => "Mantenimiento",
        AssetStatus.Retired => "Baja",
        _ => Status.ToString()
    };

    public int ModelId { get; set; }
    public int BrandId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Specs { get; set; }

    public string Description
    {
        get
        {
            var title = string.Join(" ", new[] { BrandName, ModelName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Join(" · ", new[] { title, Specs }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }

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
    /// <summary>Vacío en alta → la API genera EQ/AC-yyyy-####.</summary>
    [MaxLength(50)]
    public string? AssetCode { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    public AssetKind Kind { get; set; } = AssetKind.Equipment;

    [Range(1, int.MaxValue)]
    public int ModelId { get; set; }

    [Required]
    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;

    public Guid? SupplierId { get; set; }

    /// <summary>En alta se fuerza Available. En update aplica matriz de transiciones.</summary>
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
