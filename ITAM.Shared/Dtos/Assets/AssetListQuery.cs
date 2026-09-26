using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Assets;

/// <summary>
/// Filtros facetados (estilo ecommerce): cada dimensión acepta varios valores (OR dentro, AND entre dimensiones).
/// Ejemplo:
/// <c>?search=DELL&amp;statuses=Disponible&amp;statuses=Asignado&amp;categories=Laptop&amp;categories=Monitor&amp;kinds=1&amp;locationIds=2&amp;locationIds=5</c>
/// </summary>
public sealed class AssetListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Texto libre: código, serie, IMEI, marca, modelo, specs, categoría, colaborador.</summary>
    public string? Search { get; set; }

    /// <summary>Compat práctica: un solo status. Se fusiona en <see cref="Statuses"/>.</summary>
    public string? Status { get; set; }

    /// <summary>Varios estados (ES / enum / número). OR.</summary>
    public string[]? Statuses { get; set; }

    /// <summary>Compat: una categoría por nombre. Se fusiona en <see cref="Categories"/>.</summary>
    public string? Category { get; set; }

    /// <summary>Varios nombres de categoría (contains). OR.</summary>
    public string[]? Categories { get; set; }

    /// <summary>Compat: un tipo. Se fusiona en <see cref="Kinds"/>.</summary>
    public AssetKind? Kind { get; set; }

    /// <summary>Varios tipos (Equipment / Accessory). OR.</summary>
    public AssetKind[]? Kinds { get; set; }

    /// <summary>Compat: un modelo. Se fusiona en <see cref="ModelIds"/>.</summary>
    public int? ModelId { get; set; }

    /// <summary>Varios modelos. OR.</summary>
    public int[]? ModelIds { get; set; }

    /// <summary>Compat: una ubicación. Se fusiona en <see cref="LocationIds"/>.</summary>
    public int? LocationId { get; set; }

    /// <summary>Varias ubicaciones. OR.</summary>
    public int[]? LocationIds { get; set; }

    /// <summary>Varios Ids de categoría. OR.</summary>
    public int[]? CategoryIds { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public void Normalize()
    {
        Page = Page < 1 ? 1 : Page;
        PageSize = PageSize < 1
            ? DefaultPageSize
            : Math.Min(PageSize, MaxPageSize);

        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

        Statuses = MergeStrings(Statuses, Status);
        Categories = MergeStrings(Categories, Category);
        Status = null;
        Category = null;

        Kinds = MergeEnums(Kinds, Kind);
        Kind = null;

        ModelIds = MergeInts(ModelIds, ModelId);
        LocationIds = MergeInts(LocationIds, LocationId);
        CategoryIds = DistinctPositive(CategoryIds);

        ModelId = null;
        LocationId = null;
    }

    private static string[]? MergeStrings(string[]? many, string? one)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (many is not null)
        {
            foreach (var s in many)
            {
                if (!string.IsNullOrWhiteSpace(s))
                    set.Add(s.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(one))
            set.Add(one.Trim());

        return set.Count == 0 ? null : set.ToArray();
    }

    private static AssetKind[]? MergeEnums(AssetKind[]? many, AssetKind? one)
    {
        var set = new HashSet<AssetKind>();
        if (many is not null)
        {
            foreach (var k in many)
            {
                if (Enum.IsDefined(k))
                    set.Add(k);
            }
        }

        if (one.HasValue && Enum.IsDefined(one.Value))
            set.Add(one.Value);

        return set.Count == 0 ? null : set.ToArray();
    }

    private static int[]? MergeInts(int[]? many, int? one)
    {
        var set = new HashSet<int>();
        if (many is not null)
        {
            foreach (var id in many)
            {
                if (id > 0)
                    set.Add(id);
            }
        }

        if (one is > 0)
            set.Add(one.Value);

        return set.Count == 0 ? null : set.ToArray();
    }

    private static int[]? DistinctPositive(int[]? ids)
    {
        if (ids is null || ids.Length == 0)
            return null;
        var set = ids.Where(id => id > 0).Distinct().ToArray();
        return set.Length == 0 ? null : set;
    }
}
