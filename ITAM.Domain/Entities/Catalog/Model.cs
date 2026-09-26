namespace ITAM.Domain.Entities.Catalog;

public class Model
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? Specs { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
