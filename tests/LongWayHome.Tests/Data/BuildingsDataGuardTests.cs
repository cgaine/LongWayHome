using LongWayHome.Application.Data;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #163: the <c>buildings.json</c> load errors the acceptance tests don't reach: null rows, lists and text, and a
/// material listed twice in one row's cost.
/// </summary>
public class BuildingsDataGuardTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    private static string Shipped(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName));

    private static IReadOnlyList<string> LoadErrors(string buildings) =>
        Assert.Throws<GameDataException>(() => new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            Shipped(JsonGameDataSource.MaterialsFileName),
            buildings).Load()).Errors;

    private static string Row(string costItems = """[{ "materialId": "wood", "quantity": 5 }]""", string unlocks = "\"Two\"") =>
        $$"""{ "buildingId": "general_store", "stage": 2, "costItems": {{costItems}}, "costGold": 10, "unlocks": {{unlocks}} }""";

    [Theory]
    [InlineData("""{ "stages": null }""", "stages must be a list")]
    [InlineData("""{ "stages": [null] }""", "stages[0] must be an object")]
    public void A_null_stages_list_or_row_is_refused(string buildings, string mentions)
    {
        var error = Assert.Single(LoadErrors(buildings));

        Assert.Equal($"buildings.json: {mentions}", error[..$"buildings.json: {mentions}".Length]);
    }

    [Fact]
    public void A_row_with_null_cost_items_is_refused()
    {
        var error = Assert.Single(LoadErrors($$"""{ "stages": [{{Row(costItems: "null")}}] }"""));

        Assert.Contains("costItems must be a list", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_null_cost_item_is_refused()
    {
        var error = Assert.Single(LoadErrors($$"""{ "stages": [{{Row(costItems: "[null]")}}] }"""));

        Assert.Contains("costItems[0] must be an object", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_material_listed_twice_in_one_cost_is_refused()
    {
        var costItems = """[{ "materialId": "wood", "quantity": 5 }, { "materialId": "wood", "quantity": 2 }]""";

        var error = Assert.Single(LoadErrors($$"""{ "stages": [{{Row(costItems: costItems)}}] }"""));

        Assert.Contains("lists wood more than once", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_null_unlocks_text_is_refused()
    {
        var error = Assert.Single(LoadErrors($$"""{ "stages": [{{Row(unlocks: "null")}}] }"""));

        Assert.Contains("unlocks must be text", error, StringComparison.Ordinal);
    }

    [Fact]
    public void The_six_file_constructor_refuses_a_null_buildings_file()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonGameDataSource("{}", "{}", "{}", "{}", "{}", null!));
    }

    [Fact]
    public void A_valid_row_loads_as_a_stage_with_its_cost()
    {
        var data = new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            Shipped(JsonGameDataSource.MaterialsFileName),
            $$"""{ "stages": [{{Row()}}] }""").Load();

        var row = Assert.Single(data.BuildingStages);
        Assert.Equal(10, row.CostGold.Value);
        Assert.Equal("Two", row.Unlocks);
    }
}
