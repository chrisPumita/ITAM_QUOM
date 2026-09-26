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
    [InlineData(CustodyFormStatus.Draft, "Borrador")]
    [InlineData(CustodyFormStatus.Issued, "Emitida")]
    [InlineData(CustodyFormStatus.Signed, "Firmada")]
    [InlineData(CustodyFormStatus.Closed, "Cerrada")]
    public void CustodyFormStatus_ToSpanish(CustodyFormStatus value, string expected)
        => Assert.Equal(expected, value.ToSpanish());
}
