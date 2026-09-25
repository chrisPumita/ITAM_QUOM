namespace ITAM.Domain.Entities.Catalog;

/// <summary>
/// Modelo comercial (categoría + marca). Define el tipo concreto del activo.
/// </summary>
public class Model
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Especificaciones libres (CPU, RAM, SO…).</summary>
    public string? Specs { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
