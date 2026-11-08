using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #154 paths around <see cref="ForestPathSaveTests"/>: a bad <c>nodeAreasPlaced</c> is refused like other bad values;
/// topping an Area up never doubles a node already standing on a spawn point's Tile; an Area left out is topped up
/// whichever it is (the farm onto grass only); a store with no forest spawn points writes only the farm; a pre-#151
/// file places each forest path node once.
/// </summary>
public sealed class ForestPathSaveTopUpTests : IDisposable
{
    private static readonly NodeSpawn[] Spawns =
    [
        NodeGames.Spawn(2, 2, NodeKind.Tree),
        NodeGames.Spawn(29, 8, NodeKind.Rock),
        ForestSpawn(3, 38, NodeKind.Tree),
    ];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store(params NodeSpawn[] spawns) => new(_folder.Path, spawns.Length == 0 ? Spawns : spawns);

    private static NodeSpawn ForestSpawn(int x, int y, NodeKind kind) =>
        new(
            $"forest_path-{x}-{y}", AreaId.ForestPath, null, AreaGrid.Position(AreaId.ForestPath, x, y), kind, true, true);

    private static GameState GameWith(params ResourceNode[] nodes) => NodeGames.WithNodes(SaveGames.Rich(), nodes);

    private void SaveAndEdit(GameState game, Action<JsonObject> edit)
    {
        Store().Save(1, game);
        var document = _folder.ReadSlot(1);
        edit(document);
        _folder.WriteSlot(1, document);
    }

    [Theory]
    [InlineData("forest")]
    [InlineData("Farm")]
    [InlineData("")]
    public void An_unknown_area_code_in_nodeAreasPlaced_is_refused(string code)
    {
        SaveAndEdit(GameWith(), document => document["nodeAreasPlaced"] = new JsonArray("farm", code));

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    [Fact]
    public void A_null_entry_in_nodeAreasPlaced_is_refused()
    {
        SaveAndEdit(GameWith(), document => document["nodeAreasPlaced"] = new JsonArray("farm", null));

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    [Fact]
    public void A_forest_path_node_already_in_the_file_is_not_placed_twice()
    {
        var standing = ResourceNode.FromSpawn(Spawns[2]);
        SaveAndEdit(GameWith(standing), document => document.Remove("nodeAreasPlaced"));

        var loaded = Store().Load(1);

        Assert.Single(loaded.ResourceNodes, node => node.Area == AreaId.ForestPath);
    }

    [Fact]
    public void A_file_listing_only_the_forest_path_gets_the_farm_nodes_on_grass_only()
    {
        // The farm tree's Tile (2, 2) is tilled, so only the rock comes back.
        var tilled = TestGames.TilledWith(new GridPosition(2, 2), null);
        var game = TestGames.WithTiles(150, null, tilled);
        SaveAndEdit(game, document => document["nodeAreasPlaced"] = new JsonArray("forest_path"));

        var loaded = Store().Load(1);

        Assert.Equal(["farm-29-8:Rock:Farm:29:8:0"], NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void A_store_without_forest_path_spawn_points_lists_only_the_farm()
    {
        var store = Store(NodeGames.Spawn(2, 2, NodeKind.Tree));
        store.Save(1, GameWith());

        var areas = _folder.ReadSlot(1)["nodeAreasPlaced"]!.AsArray().Select(code => code!.GetValue<string>());

        Assert.Equal(["farm"], areas);
    }

    [Fact]
    public void A_file_from_before_resource_nodes_places_each_forest_path_node_once()
    {
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack()));

        var loaded = Store().Load(1);

        Assert.Equal(
            ["forest_path-3-38:Tree:ForestPath:3:38:0"],
            NodeGames.Describe(loaded.ResourceNodes.Where(node => node.Area == AreaId.ForestPath)));
    }

    [Fact]
    public void A_forest_path_node_on_row_38_survives_a_save_and_load()
    {
        Store().Save(1, GameWith(ResourceNode.FromSpawn(Spawns[2])));

        var loaded = Store().Load(1);

        var node = Assert.Single(loaded.ResourceNodes);
        Assert.Equal(AreaGrid.Position(AreaId.ForestPath, 3, 38), node.Position);
    }
}
