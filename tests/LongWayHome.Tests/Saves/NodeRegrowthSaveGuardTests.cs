using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #153 path the acceptance tests don't drive: a restored ResourceNode takes whether it grows back from its spawn
/// point in <c>nodes.json</c> (BR-N4, BR-N5), not from its kind, so a load never turns a non-respawning node into one.
/// </summary>
public sealed class NodeRegrowthSaveGuardTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void BR_N5_a_restored_node_takes_respawns_from_its_spawn_point()
    {
        // A tree spawn point marked not to respawn: the kind's default (true) would be wrong.
        var once = new NodeSpawn("farm-2-2", AreaId.Farm, null, new GridPosition(2, 2), NodeKind.Tree, true, Respawns: false);
        var store = new JsonSaveStore(_folder.Path, [once]);
        store.Save(1, NodeGames.WithNodes(SaveGames.Rich(), ResourceNode.FromSpawn(once)));

        var loaded = store.Load(1);

        Assert.False(Assert.Single(loaded.ResourceNodes).Respawns);
    }
}
