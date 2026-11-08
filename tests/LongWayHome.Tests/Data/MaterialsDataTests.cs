using System.Text.Json.Nodes;
using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #152: <c>materials.json</c> (<c>{ id, name, maxStack: 99 }</c> per MaterialId) loads into
/// <see cref="GameData.MaterialTypes"/> and bad rows are refused like crops; <c>tuning.json</c>'s
/// <c>swingsToClear</c> and <c>nodeYields</c> load into <see cref="GameTuning"/>. The shipped files are read from the
/// test output's <c>data/</c> folder.
/// </summary>
public class MaterialsDataTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    private static string Shipped(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName));

    private const string ValidMaterials = """
        {
          "materialTypes": [
            { "id": "wood", "name": "Wood", "maxStack": 99 },
            { "id": "stone", "name": "Stone", "maxStack": 99 }
          ]
        }
        """;

    private static GameData Load(string materials) =>
        new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            materials).Load();

    private static IReadOnlyList<string> LoadErrors(string materials) =>
        Assert.Throws<GameDataException>(() => Load(materials)).Errors;

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(json)!;
        edit(root);
        return root.ToJsonString();
    }

    // ---- materials.json ----

    [Fact]
    public void Valid_materials_load_into_material_types_keyed_by_id()
    {
        var data = Load(ValidMaterials);

        Assert.Equal(
            [new MaterialType(MaterialId.Wood, "Wood", 99), new MaterialType(MaterialId.Stone, "Stone", 99)],
            data.MaterialTypes.Values.OrderBy(type => type.Id));
        Assert.Equal(new MaterialType(MaterialId.Stone, "Stone", 99), data.MaterialTypes[MaterialId.Stone]);
    }

    [Fact]
    public void The_shipped_materials_file_has_every_material_id_with_its_canonical_name_and_max_stack_99()
    {
        var data = Load(Shipped(JsonGameDataSource.MaterialsFileName));

        Assert.Equal(Enum.GetValues<MaterialId>().Order(), data.MaterialTypes.Keys.Order());
        Assert.All(data.MaterialTypes.Values, type => Assert.Equal(99, type.MaxStack));
        Assert.Equal("Rosk ore", data.MaterialTypes[MaterialId.RoskOre].Name);
        Assert.Equal("Sealed plank", data.MaterialTypes[MaterialId.SealedPlank].Name);
        Assert.Equal("Rosk pump", data.MaterialTypes[MaterialId.RoskPump].Name);
    }

    [Fact]
    public void A_material_used_twice_is_refused_naming_it()
    {
        var materials = Edit(ValidMaterials, root => root["materialTypes"]![1]!["id"] = "wood");

        var error = Assert.Single(LoadErrors(materials));

        Assert.StartsWith("materials.json: ", error);
        Assert.Contains("wood", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_material_with_an_empty_name_is_refused_naming_the_field(string name)
    {
        var materials = Edit(ValidMaterials, root => root["materialTypes"]![0]!["name"] = name);

        var error = Assert.Single(LoadErrors(materials));

        Assert.StartsWith("materials.json: ", error);
        Assert.Contains("name", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(98)]
    [InlineData(100)]
    public void A_max_stack_other_than_99_is_refused_BR_K2(int maxStack)
    {
        var materials = Edit(ValidMaterials, root => root["materialTypes"]![1]!["maxStack"] = maxStack);

        var error = Assert.Single(LoadErrors(materials));

        Assert.StartsWith("materials.json: ", error);
        Assert.Contains("maxStack", error);
    }

    [Fact]
    public void An_unknown_material_code_is_refused_as_invalid_json()
    {
        var materials = Edit(ValidMaterials, root => root["materialTypes"]![0]!["id"] = "oak");

        var error = Assert.Single(LoadErrors(materials));

        Assert.StartsWith("materials.json: invalid JSON at ", error);
    }

    [Fact]
    public void A_null_material_list_is_refused()
    {
        var error = Assert.Single(LoadErrors("""{ "materialTypes": null }"""));

        Assert.StartsWith("materials.json: ", error);
    }

    [Fact]
    public void A_materials_file_that_does_not_parse_is_refused()
    {
        var error = Assert.Single(LoadErrors("{ not json"));

        Assert.StartsWith("materials.json: invalid JSON", error);
    }

    [Fact]
    public void A_null_materials_file_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonGameDataSource(
            Shipped(JsonGameDataSource.CropsFileName),
            Shipped(JsonGameDataSource.ShopFileName),
            Shipped(JsonGameDataSource.TuningFileName),
            Shipped(JsonGameDataSource.NodesFileName),
            null!));
    }

    // ---- tuning.json: swingsToClear and nodeYields ----

    [Fact]
    public void BR_N2_the_shipped_tuning_swings_to_clear_are_basic_4_rosk_3_groth_2()
    {
        var tuning = Load(Shipped(JsonGameDataSource.MaterialsFileName)).Tuning;

        Assert.Equal(4, tuning.SwingsToClear[ToolTier.Basic]);
        Assert.Equal(3, tuning.SwingsToClear[ToolTier.Rosk]);
        Assert.Equal(2, tuning.SwingsToClear[ToolTier.Groth]);
    }

    [Fact]
    public void BR_N3_the_shipped_tuning_node_yields_are_tree_5_wood_2_sap_rock_3_stone_stump_4_wood()
    {
        var tuning = Load(Shipped(JsonGameDataSource.MaterialsFileName)).Tuning;

        Assert.Equal(
            [new MaterialStack(MaterialId.Wood, 5), new MaterialStack(MaterialId.Sap, 2)], tuning.NodeYields[NodeKind.Tree]);
        Assert.Equal([new MaterialStack(MaterialId.Stone, 3)], tuning.NodeYields[NodeKind.Rock]);
        Assert.Equal([new MaterialStack(MaterialId.Wood, 4)], tuning.NodeYields[NodeKind.Stump]);
    }

    [Fact]
    public void BR_N2_the_shipped_session_data_carries_the_swing_tuning()
    {
        // The data every session test runs on (the four-file load) has the swing counts too.
        Assert.Equal(4, Games.TestGames.Shipped.Tuning.SwingsToClear[ToolTier.Basic]);
        Assert.Equal(3, Games.TestGames.Shipped.Tuning.NodeYields[NodeKind.Rock].Single().Quantity);
    }
}
