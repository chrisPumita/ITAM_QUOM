using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Catalog;

public class CategoryListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int? ParentCategoryId { get; set; }
}

public class CategoryUpsertDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int? ParentCategoryId { get; set; }
}
