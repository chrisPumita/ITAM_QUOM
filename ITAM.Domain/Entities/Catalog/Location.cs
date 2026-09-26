namespace ITAM.Domain.Entities.Catalog;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsWarehouse { get; set; }
    public int? ParentLocationId { get; set; }
    public Location? ParentLocation { get; set; }
    public ICollection<Location> Children { get; set; } = new List<Location>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
