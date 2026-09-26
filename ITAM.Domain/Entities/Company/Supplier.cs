namespace ITAM.Domain.Entities.Company;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public bool OffersPurchase { get; set; }

    public bool OffersMaintenance { get; set; }

    public bool OffersRental { get; set; }

    public bool IsActive { get; set; } = true;
}
