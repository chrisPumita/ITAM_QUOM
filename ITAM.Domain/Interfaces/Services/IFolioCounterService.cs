using ITAM.Shared.Enums;

namespace ITAM.Domain.Interfaces.Services;

public interface IFolioCounterService
{
    /// <summary>Emite el siguiente folio PREFIX-yyyy-#### bajo bloqueo.</summary>
    Task<string> NextAsync(string prefix, CancellationToken ct = default);
}

public static class FolioPrefixes
{
    public const string Responsiva = "RES";
    public const string Equipment = "EQ";
    public const string Accessory = "AC";

    public static string ForKind(AssetKind kind) => kind switch
    {
        AssetKind.Accessory => Accessory,
        _ => Equipment
    };
}
