using ITAM.Shared.Enums;

namespace ITAM.Tests.Shared;

public class EnumDisplayHelperTests
{
    [Theory]
    [InlineData(AssetCondition.New, "Nuevo")]
    [InlineData(AssetCondition.Used, "Usado")]
    public void AssetCondition_ToSpanish(AssetCondition value, string expected)
        => Assert.Equal(expected, value.ToSpanish());

    [Theory]
    [InlineData("Disponible", AssetStatus.Available)]
    [InlineData("disponible", AssetStatus.Available)]
    [InlineData("Assigned", AssetStatus.Assigned)]
    [InlineData("1", AssetStatus.Available)]
    [InlineData("2", AssetStatus.Assigned)]
    [InlineData("Baja", AssetStatus.Retired)]
    public void TryParseAssetStatus_AcceptsSpanishEnumAndNumber(string raw, AssetStatus expected)
    {
        Assert.True(EnumDisplayHelper.TryParseAssetStatus(raw, out var status));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void TryParseAssetStatus_RejectsUnknown()
        => Assert.False(EnumDisplayHelper.TryParseAssetStatus("Inventado", out _));
}
