using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Company;

public class SupplierListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool OffersPurchase { get; set; }
    public bool OffersMaintenance { get; set; }
    public bool OffersRental { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierUpsertDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Contact { get; set; }

    [MaxLength(256), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    public bool OffersPurchase { get; set; }
    public bool OffersMaintenance { get; set; }
    public bool OffersRental { get; set; }
    public bool IsActive { get; set; } = true;
}
