using System.Text.Json.Nodes;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #154 AC6: a save (still version 2) names the Areas whose starting nodes this Game has placed as
/// <c>nodeAreasPlaced</c>, an array of AreaId codes. A file without it counts as <c>["farm"]</c>, so a save from before
/// this feature loads with every forest path atStart node standing (its farm nodes as saved); an Area already listed
/// isn't refilled, so a cleared forest path node stays cleared.
/// </summary>
public sealed class ForestPathSaveTests : IDisposable
{
    /// <summary>Two farm spawn points, two atStart forest path ones and one forest path one that isn't atStart.</summary>
    private static readonly NodeSpawn[] Spawns =
    [
        NodeGames.Spawn(2, 2, NodeKind.Tree),
        NodeGames.Spawn(29, 8, NodeKind.Rock),
        ForestSpawn(3, 10, NodeKind.Tree),
        ForestSpawn(10, 20, NodeKind.Rock),
        ForestSpawn(2, 30, NodeKind.Tree, atStart: false),
    ];

    /// <summary>The two farm nodes, as <see cref="NodeGames.Describe"/> writes them.</summary>
    private static readonly string[] FarmNodes = ["farm-2-2:Tree:Farm:2:2:0", "farm-29-8:Rock:Farm:29:8:0"];

    /// <summary>The two atStart forest path nodes, as <see cref="NodeGames.Describe"/> writes them.</summary>
    private static readonly string[] ForestNodes =
        ["forest_path-10-20:Rock:ForestPath:10:20:0", "forest_path-3-10:Tree:ForestPath:3:10:0"];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, Spawns);

    private static NodeSpawn ForestSpawn(int x, int y, NodeKind kind, bool atStart = true) =>
        new($"forest_path-{x}-{y}", AreaId.ForestPath, null, new GridPosition(x, y), kind, atStart, Respawns: true);

    private static ResourceNode ForestNode(int x, int y, NodeKind kind) =>
        new($"forest_path-{x}-{y}", kind, AreaId.ForestPath, new GridPosition(x, y));

    /// <summary>A rich Game holding the given nodes.</summary>
    private static GameState GameWith(params ResourceNode[] nodes) => NodeGames.WithNodes(SaveGames.Rich(), nodes);

    private static List<string> Areas(JsonObject document) =>
        document["nodeAreasPlaced"]!.AsArray().Select(code => code!.GetValue<string>()).Order(StringComparer.Ordinal).ToList();

    /// <summary>Saves the Game to slot 1, then rewrites the file with <paramref name="edit"/> applied.</summary>
    private void SaveAndEdit(GameState game, Action<JsonObject> edit)
    {
        Store().Save(1, game);
        var document = _folder.ReadSlot(1);
        edit(document);
        _folder.WriteSlot(1, document);
    }

    // ---- Written saves ----

    [Fact]
    public void AC6_a_written_save_lists_farm_and_forest_path_in_nodeAreasPlaced()
    {
        Store().Save(1, GameWith(NodeGames.Node(2, 2), ForestNode(3, 10, NodeKind.Tree)));

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        Assert.True(document["nodeAreasPlaced"] is JsonArray, "the saved file has no nodeAreasPlaced array");
        Assert.Equal(["farm", "forest_path"], Areas(document));
    }

    [Fact]
    public void AC6_a_game_with_every_node_cleared_still_lists_both_areas()
    {
        Store().Save(1, GameWith());

        Assert.Equal(["farm", "forest_path"], Areas(_folder.ReadSlot(1)));
    }

    // ---- Saves from before #154 ----

    [Fact]
    public void AC6_a_save_without_nodeAreasPlaced_loads_with_every_forest_path_atStart_node_standing()
    {
        // The farm rock was cleared before the save; it stays cleared, as the farm is counted as placed.
        SaveAndEdit(GameWith(NodeGames.Node(2, 2)), document => document.Remove("nodeAreasPlaced"));

        var loaded = Store().Load(1);

        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", .. ForestNodes],
            NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC6_a_save_listing_only_the_farm_gains_the_forest_path_nodes()
    {
        SaveAndEdit(GameWith(NodeGames.Node(2, 2)), document => document["nodeAreasPlaced"] = new JsonArray("farm"));

        var loaded = Store().Load(1);

        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", .. ForestNodes],
            NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC6_a_save_without_nodeAreasPlaced_keeps_its_farm_nodes_as_saved()
    {
        SaveAndEdit(GameWith(), document => document.Remove("nodeAreasPlaced"));

        var loaded = Store().Load(1);

        Assert.DoesNotContain(loaded.ResourceNodes, node => node.Area == AreaId.Farm);
        Assert.Equal(ForestNodes, NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC6_an_old_save_loaded_and_saved_again_lists_both_areas_and_keeps_its_forest_path_nodes()
    {
        SaveAndEdit(GameWith(NodeGames.Node(2, 2)), document => document.Remove("nodeAreasPlaced"));
        var loaded = Store().Load(1);

        Store().Save(1, loaded);

        Assert.Equal(["farm", "forest_path"], Areas(_folder.ReadSlot(1)));
        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", .. ForestNodes],
            NodeGames.Describe(Store().Load(1).ResourceNodes));
    }

    [Fact]
    public void AC6_a_save_from_before_resource_nodes_loads_every_atStart_node_in_both_areas()
    {
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack()));

        var loaded = Store().Load(1);

        Assert.Equal([.. FarmNodes, .. ForestNodes], NodeGames.Describe(loaded.ResourceNodes));
    }

    // ---- Cleared forest path nodes stay cleared ----

    [Fact]
    public void AC6_a_cleared_forest_path_node_stays_cleared_across_a_save_and_load()
    {
        // forest_path-10-20 was cleared; only the tree still stands on the forest path.
        Store().Save(1, GameWith(NodeGames.Node(2, 2), ForestNode(3, 10, NodeKind.Tree)));

        var loaded = Store().Load(1);

        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", "forest_path-3-10:Tree:ForestPath:3:10:0"],
            NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC6_a_forest_path_with_every_node_cleared_is_not_refilled_on_load()
    {
        SaveAndEdit(
            GameWith(NodeGames.Node(2, 2)),
            document => document["nodeAreasPlaced"] = new JsonArray("farm", "forest_path"));

        var loaded = Store().Load(1);

        Assert.Equal(["farm-2-2:Tree:Farm:2:2:0"], NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC6_an_old_save_whose_forest_path_nodes_were_then_cleared_stays_cleared_after_the_next_save()
    {
        SaveAndEdit(GameWith(NodeGames.Node(2, 2)), document => document.Remove("nodeAreasPlaced"));
        var loaded = Store().Load(1);
        Assert.Contains(loaded.ResourceNodes, node => node.Area == AreaId.ForestPath);

        // The Player clears both forest path nodes, then the Game is saved and loaded again.
        var cleared = NodeGames.WithNodes(loaded, [.. loaded.ResourceNodes.Where(node => node.Area == AreaId.Farm)]);
        Store().Save(1, cleared);

        Assert.Equal(["farm-2-2:Tree:Farm:2:2:0"], NodeGames.Describe(Store().Load(1).ResourceNodes));
    }
}
