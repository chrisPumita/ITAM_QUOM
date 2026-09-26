using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Catalog;

public class BrandListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class BrandUpsertDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
