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
}
