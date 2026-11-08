using System.Text.Json.Nodes;
using LongWayHome.Application.Data;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #152 validation the acceptance tests don't drive: <c>tuning.json</c>'s optional <c>swingsToClear</c> and
/// <c>nodeYields</c> are refused when they name an unknown code, a count or quantity below 1, a missing tier or a null
/// list; when absent they load empty. A null row in <c>materials.json</c> is refused.
/// </summary>
public class SwingTuningDataTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    private static string Shipped(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName));

    private static GameData Load(string tuning, string? materials = null) =>
        new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            tuning,
            Shipped(JsonGameDataSource.NodesFileName),
            materials ?? Shipped(JsonGameDataSource.MaterialsFileName)).Load();

    private static string Tuning(Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(Shipped(JsonGameDataSource.TuningFileName))!;
        edit(root);
        return root.ToJsonString();
    }

    private static string SingleError(string tuning) =>
        Assert.Single(Assert.Throws<GameDataException>(() => Load(tuning)).Errors);

    [Fact]
    public void Tuning_without_swing_fields_loads_them_empty()
    {
        var tuning = Tuning(root =>
        {
            root.AsObject().Remove("swingsToClear");
            root.AsObject().Remove("nodeYields");
        });

        var data = Load(tuning);

        Assert.Empty(data.Tuning.SwingsToClear);
        Assert.Empty(data.Tuning.NodeYields);
    }

    [Fact]
    public void An_unknown_tier_in_swings_to_clear_is_refused()
    {
        var error = SingleError(Tuning(root => root["swingsToClear"]!["iron"] = 3));

        Assert.Equal("tuning.json: swingsToClear.iron is not a tool tier", error);
    }

    [Fact]
    public void A_swing_count_below_1_is_refused_BR_N2()
    {
        var error = SingleError(Tuning(root => root["swingsToClear"]!["rosk"] = 0));

        Assert.Equal("tuning.json: swingsToClear.rosk must be at least 1 (BR-N2), was 0", error);
    }

    [Fact]
    public void A_missing_tier_in_swings_to_clear_is_refused_BR_N2()
    {
        var error = SingleError(Tuning(root => root["swingsToClear"]!.AsObject().Remove("groth")));

        Assert.Equal("tuning.json: swingsToClear.groth is missing (BR-N2)", error);
    }

    [Fact]
    public void An_unknown_node_kind_in_node_yields_is_refused()
    {
        var error = SingleError(Tuning(root => root["nodeYields"]!["boulder"] = new JsonArray()));

        Assert.Equal("tuning.json: nodeYields.boulder is not a node kind", error);
    }

    [Fact]
    public void A_null_yield_list_is_refused()
    {
        var error = SingleError(Tuning(root => root["nodeYields"]!["rock"] = null));

        Assert.Equal("tuning.json: nodeYields.rock must be a list, was null", error);
    }

    [Fact]
    public void A_null_yield_stack_is_refused()
    {
        var error = SingleError(Tuning(root => root["nodeYields"]!["rock"]!.AsArray().Add(null)));

        Assert.Equal("tuning.json: nodeYields.rock[1] must be an object, was null", error);
    }

    [Fact]
    public void A_yield_quantity_below_1_is_refused_BR_N3()
    {
        var error = SingleError(Tuning(root => root["nodeYields"]!["tree"]![1]!["quantity"] = 0));

        Assert.Equal("tuning.json: nodeYields.tree[1].quantity must be at least 1 (BR-N3), was 0", error);
    }

    [Fact]
    public void A_null_material_row_is_refused()
    {
        var errors = Assert.Throws<GameDataException>(() =>
            Load(Shipped(JsonGameDataSource.TuningFileName), """{ "materialTypes": [ null ] }""")).Errors;

        Assert.Equal("materials.json: materialTypes[0] must be an object, was null", Assert.Single(errors));
    }

    [Fact]
    public void The_four_file_load_has_no_material_types()
    {
        var data = new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName)).Load();

        Assert.Empty(data.MaterialTypes);
    }
}
