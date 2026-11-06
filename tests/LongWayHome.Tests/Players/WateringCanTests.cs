using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Players;

public class WateringCanTests
{
    [Fact]
    public void BR_W1_new_can_starts_full_and_not_upgraded()
    {
        var can = new WateringCan(20);

        Assert.Equal(20, can.Capacity);
        Assert.Equal(20, can.Charges);
        Assert.False(can.Upgraded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BR_W1_can_with_capacity_below_1_throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WateringCan(capacity));
    }

    [Fact]
    public void BR_W1_can_with_capacity_1_is_full()
    {
        var can = new WateringCan(1);

        Assert.Equal(1, can.Capacity);
        Assert.Equal(1, can.Charges);
    }
}
