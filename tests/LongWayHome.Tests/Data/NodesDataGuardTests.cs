using System.Text.Json.Nodes;
using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>#151: the <c>nodes.json</c> checks the AC5 red tests don't drive: null and blank entries, other areas, the mine level.</summary>
public class NodesDataGuardTests
{
    private const string OneSpawn = """
        { "spawns": [ { "id": "farm-2-3", "area": "farm", "x": 2, "y": 3, "kind": "tree", "atStart": true, "respawns": true } ] }
        """;

    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "data");

    [Fact]
    public void Null_nodes_json_is_refused_by_the_constructor()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName), Read(JsonGameDataSource.ShopFileName), Read(JsonGameDataSource.TuningFileName), null!));
    }

    [Fact]
    public void A_null_spawn_entry_fails_the_load()
    {
        var error = Assert.Single(LoadErrors("""{ "spawns": [ null ] }"""));

        Assert.Equal("nodes.json: spawns[0] must be an object, was null", error);
    }

    [Fact]
    public void A_blank_id_fails_the_load()
    {
        var error = Assert.Single(LoadErrors(Edit(OneSpawn, spawn => spawn["id"] = " ")));

        Assert.Equal("nodes.json: spawns[0].id must not be empty", error);
    }

    [Fact]
    public void A_file_that_is_null_fails_the_load()
    {
        var error = Assert.Single(LoadErrors("null"));

        Assert.Equal("nodes.json: the file contains null instead of an object", error);
    }

    [Fact]
    public void An_empty_spawn_list_loads_with_no_spawn_points()
    {
        Assert.Empty(Load("""{ "spawns": [] }""").NodeSpawns);
    }

    [Fact]
    public void Off_the_farm_a_spawn_point_must_still_lie_on_the_grid()
    {
        var onGrid = Edit(OneSpawn, spawn => spawn["area"] = "forest_path");
        Assert.Equal(AreaId.ForestPath, Assert.Single(Load(onGrid).NodeSpawns).Area);

        var offGrid = Edit(OneSpawn, spawn =>
        {
            spawn["area"] = "town";
            spawn["x"] = 200;
        });
        var error = Assert.Single(LoadErrors(offGrid));
        Assert.Equal("nodes.json: farm-2-3.x must be between 0 and 71, was 200", error);
    }

    [Fact]
    public void A_mine_spawns_level_is_read_and_tiles_on_different_levels_do_not_clash()
    {
        const string Mine = """
            { "spawns": [
              { "id": "mine-1-2-2", "area": "mine", "mineLevel": 1, "x": 2, "y": 2, "kind": "rock", "atStart": true, "respawns": true },
              { "id": "mine-2-2-2", "area": "mine", "mineLevel": 2, "x": 2, "y": 2, "kind": "rock", "atStart": true, "respawns": true }
            ] }
            """;

        var spawns = Load(Mine).NodeSpawns;

        Assert.Equal([1, 2], spawns.Select(spawn => spawn.MineLevel));
    }

    [Fact]
    public void BR_N5_a_respawning_debris_spawn_point_fails_the_load()
    {
        // #153: debris never grows back, like a stump. The same spawn point with respawns false loads.
        static string Debris(bool respawns) => Edit(OneSpawn, spawn =>
        {
            spawn["area"] = "mine";
            spawn["mineLevel"] = 1;
            spawn["kind"] = "debris";
            spawn["respawns"] = respawns;
        });
        Assert.Single(Load(Debris(respawns: false)).NodeSpawns);

        var error = Assert.Single(LoadErrors(Debris(respawns: true)));

        Assert.Equal("nodes.json: farm-2-3.respawns must be false for debris (BR-N5)", error);
    }

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
        edit(root["spawns"]![0]!);
        return root.ToJsonString();
    }
}
