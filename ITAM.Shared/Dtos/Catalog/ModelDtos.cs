using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Catalog;

public class ModelListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specs { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ModelUpsertDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Specs { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    public bool IsActive { get; set; } = true;
}
