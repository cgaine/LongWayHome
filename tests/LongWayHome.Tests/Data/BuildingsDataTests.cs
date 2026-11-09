using System.Text.Json.Nodes;
using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #163 AC2, AC3: <c>buildings.json</c> (<c>{ stages: [{ buildingId, stage, costItems, costGold, unlocks }] }</c>) loads
/// into <see cref="GameData.BuildingStages"/>; bad rows are refused on load like <c>shop.json</c>'s (BR-B1). The other
/// files are the shipped ones, read from the test output's <c>data/</c> folder.
/// </summary>
public class BuildingsDataTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    private static string Shipped(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName));

    private const string ValidBuildings = """
        {
          "stages": [
            { "buildingId": "general_store", "stage": 2, "costItems": [{ "materialId": "wood", "quantity": 50 }, { "materialId": "stone", "quantity": 25 }], "costGold": 300, "unlocks": "Two" },
            { "buildingId": "general_store", "stage": 3, "costItems": [{ "materialId": "plank", "quantity": 40 }], "costGold": 1200, "unlocks": "Three" },
            { "buildingId": "carpenter", "stage": 1, "costItems": [{ "materialId": "wood", "quantity": 10 }], "costGold": 0, "unlocks": "Carpenter one" }
          ]
        }
        """;

    private static GameData Load(string buildings) =>
        new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            Shipped(JsonGameDataSource.MaterialsFileName),
            buildings).Load();

    private static IReadOnlyList<string> LoadErrors(string buildings) =>
        Assert.Throws<GameDataException>(() => Load(buildings)).Errors;

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(json)!;
        edit(root);
        return root.ToJsonString();
    }

    private static string Describe(BuildingStage row) =>
        $"{row.BuildingId}:{row.Stage}:" +
        string.Join(";", row.CostItems.Select(item => $"{item.Material}={item.Quantity}")) +
        $":{row.CostGold.Value}";

    private static void AssertOneBuildingsError(string buildings, string mentions)
    {
        var errors = LoadErrors(buildings);
        var error = Assert.Single(errors);
        Assert.StartsWith("buildings.json: ", error, StringComparison.Ordinal);
        Assert.Contains(mentions, error, StringComparison.Ordinal);
    }

    // ---- AC2: the shipped file ----

    [Fact]
    public void AC2_The_shipped_buildings_file_loads_the_general_stores_three_stage_rows()
    {
        var data = Load(Shipped(JsonGameDataSource.BuildingsFileName));

        Assert.Equal(
            [
                "GeneralStore:2:Wood=50;Stone=25:300",
                "GeneralStore:3:Plank=40;Stone=60:1200",
                "GeneralStore:4:Plank=80;Stone=120;RoskOre=20:3000",
            ],
            data.BuildingStages.Select(Describe));
    }

    [Fact]
    public void AC2_The_shipped_rows_carry_their_unlocks_text()
    {
        var data = Load(Shipped(JsonGameDataSource.BuildingsFileName));

        Assert.Equal(
            [
                "The whole store reopens: the 24-slot backpack",
                "A storeroom: the 36-slot backpack and chests",
                "An expanded shop: a shipping bin for the farm",
            ],
            data.BuildingStages.Select(row => row.Unlocks));
    }

    [Fact]
    public void AC2_Valid_rows_load_into_building_stages_in_file_order()
    {
        var data = Load(ValidBuildings);

        Assert.Equal(
            ["GeneralStore:2:Wood=50;Stone=25:300", "GeneralStore:3:Plank=40:1200", "Carpenter:1:Wood=10:0"],
            data.BuildingStages.Select(Describe));
        Assert.Equal(["Two", "Three", "Carpenter one"], data.BuildingStages.Select(row => row.Unlocks));
    }

    // ---- AC3 / BR-B1 ----

    [Fact]
    public void AC3_A_gap_in_a_buildings_stages_is_refused()
    {
        // general_store 2 and 4 only.
        var buildings = Edit(ValidBuildings, root => root["stages"]![1]!["stage"] = 4);

        AssertOneBuildingsError(buildings, "general_store");
    }

    [Fact]
    public void AC3_An_unknown_material_id_is_refused()
    {
        var buildings = Edit(ValidBuildings, root => root["stages"]![0]!["costItems"]![1]!["materialId"] = "marble");

        var errors = LoadErrors(buildings);

        Assert.Contains(errors, error => error.StartsWith("buildings.json: ", StringComparison.Ordinal));
    }

    [Fact]
    public void BR_B1_Stages_that_do_not_start_right_after_the_buildings_start_stage_are_refused()
    {
        // The store starts at stage 1, so its first row must be stage 2; here it's 3 then 4.
        var buildings = Edit(ValidBuildings, root =>
        {
            root["stages"]![0]!["stage"] = 3;
            root["stages"]![1]!["stage"] = 4;
        });

        AssertOneBuildingsError(buildings, "general_store");
    }

    [Fact]
    public void BR_B1_A_carpenter_row_that_skips_its_first_stage_is_refused()
    {
        // The carpenter starts at stage 0, so its first row must be stage 1.
        var buildings = Edit(ValidBuildings, root => root["stages"]![2]!["stage"] = 2);

        AssertOneBuildingsError(buildings, "carpenter");
    }

    [Fact]
    public void BR_B1_A_duplicate_building_and_stage_is_refused()
    {
        var buildings = Edit(ValidBuildings, root => root["stages"]![1]!["stage"] = 2);

        var errors = LoadErrors(buildings);

        Assert.Contains(errors, error => error.StartsWith("buildings.json: ", StringComparison.Ordinal) && error.Contains("general_store"));
    }

    [Fact]
    public void An_unknown_building_id_is_refused()
    {
        var buildings = Edit(ValidBuildings, root => root["stages"]![2]!["buildingId"] = "bakery");

        var errors = LoadErrors(buildings);

        Assert.Contains(errors, error => error.StartsWith("buildings.json: ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_stage_below_1_is_refused(int stage)
    {
        var buildings = Edit(ValidBuildings, root => root["stages"]![2]!["stage"] = stage);

        var errors = LoadErrors(buildings);

        Assert.Contains(errors, error => error.StartsWith("buildings.json: ", StringComparison.Ordinal) && error.Contains("stage"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_cost_quantity_below_1_is_refused(int quantity)
    {
        var buildings = Edit(ValidBuildings, root => root["stages"]![0]!["costItems"]![0]!["quantity"] = quantity);

        AssertOneBuildingsError(buildings, "quantity");
    }

    [Fact]
    public void A_negative_cost_gold_is_refused()
    {
        var buildings = Edit(ValidBuildings, root => root["stages"]![1]!["costGold"] = -1);

        AssertOneBuildingsError(buildings, "costGold");
    }

    [Fact]
    public void A_file_with_no_stages_loads_none()
    {
        var data = Load("""{ "stages": [] }""");

        Assert.Empty(data.BuildingStages);
    }

    [Fact]
    public void The_five_file_constructor_still_loads_with_no_building_stages()
    {
        // Characterization (#163 keeps the older constructors): passes before the six-file constructor exists.
        var data = new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            Shipped(JsonGameDataSource.MaterialsFileName)).Load();

        Assert.Empty(data.BuildingStages);
    }
}
