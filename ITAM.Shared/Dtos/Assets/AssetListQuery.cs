using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Assets;

public sealed class AssetListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public string? Search { get; set; }

    public string? Status { get; set; }

    public string[]? Statuses { get; set; }

    public string? Category { get; set; }

    public string[]? Categories { get; set; }

    public AssetKind? Kind { get; set; }

    public AssetKind[]? Kinds { get; set; }

    public int? ModelId { get; set; }

    public int[]? ModelIds { get; set; }

    public int? LocationId { get; set; }

    public int[]? LocationIds { get; set; }

    public int[]? CategoryIds { get; set; }

    public int? BrandId { get; set; }

    public int[]? BrandIds { get; set; }

    public AssetCondition? Condition { get; set; }

    public AssetCondition[]? Conditions { get; set; }

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

        Conditions = MergeConditions(Conditions, Condition);
        Condition = null;

        ModelIds = MergeInts(ModelIds, ModelId);
        LocationIds = MergeInts(LocationIds, LocationId);
        CategoryIds = DistinctPositive(CategoryIds);
        BrandIds = MergeInts(BrandIds, BrandId);

        ModelId = null;
        LocationId = null;
        BrandId = null;
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

    private static AssetCondition[]? MergeConditions(AssetCondition[]? many, AssetCondition? one)
    {
        var set = new HashSet<AssetCondition>();
        if (many is not null)
        {
            foreach (var c in many)
            {
                if (Enum.IsDefined(c))
                    set.Add(c);
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
