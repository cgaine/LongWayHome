using System.Text.Json.Nodes;
using LongWayHome.Application.Data;
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

    private static GameData LoadTuning(Action<JsonObject> edit)
    {
        var tuning = JsonNode.Parse(Shipped(JsonGameDataSource.TuningFileName))!.AsObject();
        edit(tuning);
        return new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            tuning.ToJsonString(),
            Shipped(JsonGameDataSource.NodesFileName),
            Shipped(JsonGameDataSource.MaterialsFileName)).Load();
    }

    [Fact]
    public void BR_N4_node_regrowth_nights_loads_from_the_tuning_and_is_2_when_absent()
    {
        var data = LoadTuning(tuning => tuning["nodeRegrowthNights"] = 5);
        Assert.Equal(5, data.Tuning.NodeRegrowthNights);

        Assert.Equal(2, LoadTuning(tuning => tuning.Remove("nodeRegrowthNights")).Tuning.NodeRegrowthNights);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void BR_N4_node_regrowth_nights_below_1_is_refused(int nights)
    {
        var exception = Assert.Throws<GameDataException>(() => LoadTuning(tuning => tuning["nodeRegrowthNights"] = nights));

        Assert.Equal(
            $"tuning.json: nodeRegrowthNights must be at least 1 (BR-N4), was {nights}",
            Assert.Single(exception.Errors));
    }
}
