using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>#151: the save paths the red tests don't drive: a repeated spawn id, no spawn list, the saved kind.</summary>
public sealed class ResourceNodeSaveGuardTests : IDisposable
{
    private static readonly NodeSpawn[] Spawns = [NodeGames.Spawn(2, 2), NodeGames.Spawn(42, 0, NodeKind.Stump)];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void A_save_naming_one_spawn_point_twice_is_refused()
    {
        var store = new JsonSaveStore(_folder.Path, Spawns);
        store.Save(1, SaveGames.Rich());
        var document = _folder.ReadSlot(1);
        document["resourceNodes"] = new JsonArray(
            new JsonObject { ["spawnId"] = "farm-2-2", ["kind"] = "tree", ["swingsTaken"] = 0 },
            new JsonObject { ["spawnId"] = "farm-2-2", ["kind"] = "tree", ["swingsTaken"] = 1 });
        _folder.WriteSlot(1, document);

        Assert.Throws<SaveStoreException>(() => store.Load(1));
    }

    [Fact]
    public void A_store_with_no_spawn_list_loads_no_nodes()
    {
        new JsonSaveStore(_folder.Path, Spawns).Save(1, NodeGames.WithNodes(SaveGames.Rich(), NodeGames.Node(2, 2)));

        var loaded = new JsonSaveStore(_folder.Path).Load(1);

        Assert.Empty(loaded.ResourceNodes);
    }

    [Fact]
    public void A_loaded_node_keeps_its_saved_kind_on_its_spawn_points_tile()
    {
        var store = new JsonSaveStore(_folder.Path, Spawns);
        store.Save(1, SaveGames.Rich());
        var document = _folder.ReadSlot(1);
        document["resourceNodes"] = new JsonArray(
            new JsonObject { ["spawnId"] = "farm-2-2", ["kind"] = "stump", ["swingsTaken"] = 3 });
        _folder.WriteSlot(1, document);

        var loaded = store.Load(1);

        Assert.Equal(["farm-2-2:Stump:Farm:2:2:3"], NodeGames.Describe(loaded.ResourceNodes));
    }
}
