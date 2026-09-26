using ITAM.Shared.Enums;

namespace ITAM.Domain.Interfaces.Repositories.Assets;

public sealed class AssetFilterCriteria
{
    public string? Search { get; init; }
    public IReadOnlyList<AssetStatus>? Statuses { get; init; }
    public IReadOnlyList<AssetCondition>? Conditions { get; init; }
    public IReadOnlyList<string>? CategoryNames { get; init; }
    public IReadOnlyList<AssetKind>? Kinds { get; init; }
    public IReadOnlyList<int>? ModelIds { get; init; }
    public IReadOnlyList<int>? LocationIds { get; init; }
    public IReadOnlyList<int>? CategoryIds { get; init; }
    public IReadOnlyList<int>? BrandIds { get; init; }
}
