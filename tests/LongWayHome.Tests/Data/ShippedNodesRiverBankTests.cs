using LongWayHome.Domain.Games;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #151: the shipped <c>nodes.json</c> also keeps the grid's east column (X 71) clear, so the farmer can walk the
/// river bank and reach the farm's river water points from its north end to its south end on day one.
/// </summary>
public class ShippedNodesRiverBankTests
{
    [Fact]
    public void No_farm_spawn_point_stands_on_the_river_bank_column()
    {
        var onBank = TestGames.Shipped.NodeSpawns
            .Where(spawn => spawn.Area == AreaId.Farm && spawn.Position.X == 71)
            .Select(spawn => spawn.Id)
            .ToList();

        Assert.NotEmpty(TestGames.Shipped.NodeSpawns);
        Assert.True(onBank.Count == 0, $"spawn points on the river bank (X 71): {string.Join(", ", onBank)}");
    }
}
