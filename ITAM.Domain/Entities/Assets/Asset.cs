using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Entities.Company;
using ITAM.Domain.Enums;

namespace ITAM.Domain.Entities.Assets;

/// <summary>
/// Activo de TI (equipo o accesorio). Equipo suele llevar serie; accesorio puede no tenerla.
/// </summary>
public class Asset : BaseEntity
{
    /// <summary>Código de inventario único (etiqueta).</summary>
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>Número de serie del fabricante; opcional en accesorios.</summary>
    public string? SerialNumber { get; set; }

    public AssetKind Kind { get; set; } = AssetKind.Equipment;

    public int ModelId { get; set; }
    public Model Model { get; set; } = null!;

    public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned;

    /// <summary>Obligatorio cuando OwnershipType = Rented.</summary>
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? RentalEndDate { get; set; }
    public string? Imei { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }
    public string? ContractNumber { get; set; }

    /// <summary>Denormalizado: colaborador actual. Fuente de verdad = Assignment activa.</summary>
    public Guid? CurrentEmployeeId { get; set; }
    public Employee? CurrentEmployee { get; set; }

    /// <summary>Token de concurrencia optimista (SQL rowversion).</summary>
    public byte[] RowVersion { get; set; } = [];
}
