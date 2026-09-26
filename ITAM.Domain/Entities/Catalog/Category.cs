namespace ITAM.Domain.Entities.Catalog;

/// <summary>
/// Catálogo de categorías de activo (Laptop, Monitor, Accesorio, etc.).
/// PK int consecutivos. Soporte opcional de jerarquía padre-hijo.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
