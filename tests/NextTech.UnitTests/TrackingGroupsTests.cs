using NextTech.Application.Modules.Orders;

namespace NextTech.UnitTests;

public sealed class TrackingGroupsTests
{
    [Fact]
    public void ForOrder_TrimsCodigo()
    {
        Assert.Equal("order:ORD-656E6098", TrackingGroups.ForOrder("  ORD-656E6098  "));
    }
}
