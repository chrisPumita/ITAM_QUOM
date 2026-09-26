using System.ComponentModel.DataAnnotations;
using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Assets;

/// <summary>Fila de importación amigable: nombres, no Ids.</summary>
public class AssetImportRowDto
{
    public int RowNumber { get; set; }
    public AssetKind Kind { get; set; } = AssetKind.Equipment;

    /// <summary>Marca (se normaliza a MAYÚSCULAS; se crea si no existe).</summary>
    public string BrandName { get; set; } = string.Empty;

    /// <summary>Modelo (se crea si no existe bajo la marca).</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Categoría (default Laptops si vacío; se crea si no existe).</summary>
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
