using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Catalog;

public class LocationListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsWarehouse { get; set; }
    public int? ParentLocationId { get; set; }
    public string? ParentLocationName { get; set; }
    public bool IsActive { get; set; }
}

public class LocationUpsertDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public bool IsWarehouse { get; set; }

    public int? ParentLocationId { get; set; }

    public bool IsActive { get; set; } = true;
}
