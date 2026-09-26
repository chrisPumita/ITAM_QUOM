namespace ITAM.Domain.Entities.Company;

/// <summary>
/// Proveedor que vende, arrienda o da mantenimiento a activos TI.
/// </summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>Ofrece servicio de compra / suministro.</summary>
    public bool OffersPurchase { get; set; }

    /// <summary>Ofrece mantenimiento.</summary>
    public bool OffersMaintenance { get; set; }

    /// <summary>Ofrece arrendamiento.</summary>
    public bool OffersRental { get; set; }

    public bool IsActive { get; set; } = true;
}
