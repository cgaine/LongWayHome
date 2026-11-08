using System.Text.Json.Nodes;
using LongWayHome.Application.Data;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #151 AC5: <c>nodes.json</c> is parsed and validated on load like <c>crops.json</c>. Each test starts from a small
/// valid spawn list, with the shipped crops, shop and tuning files, and breaks exactly one thing in it.
/// </summary>
public class NodesDataTests
{
    private const string ValidNodes = """
        {
          "spawns": [
            { "id": "farm-2-3", "area": "farm", "x": 2, "y": 3, "kind": "tree", "atStart": true, "respawns": true },
            { "id": "farm-60-30", "area": "farm", "x": 60, "y": 30, "kind": "stump", "atStart": true, "respawns": false },
            { "id": "farm-5-7", "area": "farm", "x": 5, "y": 7, "kind": "rock", "atStart": false, "respawns": true }
          ]
        }
        """;

    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    [Fact]
    public void AC5_valid_nodes_load_and_map_to_node_spawns()
    {
        var data = Load(ValidNodes);

        Assert.Equal(
            [
                new NodeSpawn("farm-2-3", AreaId.Farm, null, new GridPosition(2, 3), NodeKind.Tree, true, true),
                new NodeSpawn("farm-60-30", AreaId.Farm, null, new GridPosition(60, 30), NodeKind.Stump, true, false),
                new NodeSpawn("farm-5-7", AreaId.Farm, null, new GridPosition(5, 7), NodeKind.Rock, false, true),
            ],
            data.NodeSpawns);
    }

    [Fact]
    public void AC5_snake_case_node_kinds_parse_to_their_enum_members()
    {
        var nodes = Edit(ValidNodes, root =>
        {
            root["spawns"]![2]!["kind"] = "rosk_deposit";
            root["spawns"]![2]!["area"] = "forest_path";
        });

        var spawn = Load(nodes).NodeSpawns.SingleOrDefault(spawn => spawn.Id == "farm-5-7");

        Assert.NotNull(spawn);
        Assert.Equal(NodeKind.RoskDeposit, spawn.Kind);
        Assert.Equal(AreaId.ForestPath, spawn.Area);
    }

    [Fact]
    public void AC5_unknown_kind_fails_the_load()
    {
        var nodes = Edit(ValidNodes, root => root["spawns"]![0]!["kind"] = "boulder");

        var error = Assert.Single(LoadErrors(nodes));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("kind", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_unknown_area_fails_the_load()
    {
        var nodes = Edit(ValidNodes, root => root["spawns"]![1]!["area"] = "the_moon");

        var error = Assert.Single(LoadErrors(nodes));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("area", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_duplicate_id_fails_the_load()
    {
        var nodes = Edit(ValidNodes, root => root["spawns"]![2]!["id"] = "farm-2-3");

        var error = Assert.Single(LoadErrors(nodes));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("farm-2-3", error, StringComparison.Ordinal);
        Assert.Contains("id", error, StringComparison.Ordinal);
        Assert.Contains("more than one", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_two_spawns_on_one_tile_fail_the_load()
    {
        var nodes = Edit(ValidNodes, root =>
        {
            root["spawns"]![2]!["x"] = 2;
            root["spawns"]![2]!["y"] = 3;
        });

        var error = Assert.Single(LoadErrors(nodes));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("farm-5-7", error, StringComparison.Ordinal);
        Assert.Contains("farm-2-3", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("x", -1)]
    [InlineData("x", 72)]
    [InlineData("y", -1)]
    [InlineData("y", 36)]
    public void AC5_off_grid_position_fails_the_load(string field, int value)
    {
        var nodes = Edit(ValidNodes, root => root["spawns"]![1]![field] = value);

        var error = Assert.Single(LoadErrors(nodes));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("farm-60-30", error, StringComparison.Ordinal);
        Assert.Contains(field, error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_respawning_stump_fails_the_load()
    {
        var nodes = Edit(ValidNodes, root => root["spawns"]![1]!["respawns"] = true);

        var error = Assert.Single(LoadErrors(nodes));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("farm-60-30", error, StringComparison.Ordinal);
        Assert.Contains("respawns", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_nodes_json_that_is_not_json_fails_the_load()
    {
        var error = Assert.Single(LoadErrors("{ \"spawns\": [ "));

        Assert.StartsWith("nodes.json: invalid JSON at ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_nodes_json_with_spawns_null_fails_the_load()
    {
        var error = Assert.Single(LoadErrors("""{ "spawns": null }"""));

        Assert.StartsWith("nodes.json: ", error, StringComparison.Ordinal);
        Assert.Contains("spawns", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_errors_in_nodes_json_are_collected_with_the_other_files_errors()
    {
        var nodes = Edit(ValidNodes, root => root["spawns"]![1]!["respawns"] = true);
        var tuning = Edit(Read(JsonGameDataSource.TuningFileName), root => root["maxEnergy"] = 0);

        var errors = Assert.Throws<GameDataException>(() => new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            tuning,
            nodes).Load()).Errors;

        Assert.Contains(errors, error => error.StartsWith("tuning.json: maxEnergy", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.StartsWith("nodes.json: ", StringComparison.Ordinal) && error.Contains("respawns", StringComparison.Ordinal));
    }

    // ---- Helpers -------------------------------------------------------------------------------------

    private static string Read(string fileName) => File.ReadAllText(Path.Combine(Folder, fileName));

    private static GameData Load(string nodes) =>
        new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName),
            nodes).Load();

    private static IReadOnlyList<string> LoadErrors(string nodes) =>
        Assert.Throws<GameDataException>(() => Load(nodes)).Errors;

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(json)!;
        edit(root);
        return root.ToJsonString();
    }
}
