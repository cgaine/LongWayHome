using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>#153: <c>tuning.json</c>'s <c>nodeRegrowthNights</c>, the Nights a cleared node waits (BR-N4).</summary>
public class NodeRegrowthTuningDataTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    private static string Shipped(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName));

    [Fact]
    public void BR_N4_the_shipped_tuning_makes_a_cleared_node_wait_2_nights()
    {
        var data = new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            Shipped(JsonGameDataSource.MaterialsFileName)).Load();

        Assert.Equal(2, data.Tuning.NodeRegrowthNights);
    }
}
