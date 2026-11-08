using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Spawn points, ResourceNodes and Games holding them, for #151's node tests.</summary>
internal static class NodeGames
{
    /// <summary>A farm spawn point at (x, y) with the id <c>farm-&lt;x&gt;-&lt;y&gt;</c>; trees and rocks respawn, stumps don't.</summary>
    public static NodeSpawn Spawn(int x, int y, NodeKind kind = NodeKind.Tree, bool atStart = true) =>
        new($"farm-{x}-{y}", AreaId.Farm, null, new GridPosition(x, y), kind, atStart, Respawns: kind != NodeKind.Stump);

    /// <summary>A ResourceNode standing at a farm spawn point at (x, y), id <c>farm-&lt;x&gt;-&lt;y&gt;</c>.</summary>
    public static ResourceNode Node(int x, int y, NodeKind kind = NodeKind.Tree, int swingsTaken = 0) =>
        new($"farm-{x}-{y}", kind, AreaId.Farm, new GridPosition(x, y), swingsTaken);

    /// <summary>The same Game rebuilt through the constructor with the given ResourceNodes.</summary>
    public static GameState WithNodes(GameState game, params ResourceNode[] nodes) =>
        new(
            game.Id,
            game.Date,
            game.ClockMinutes,
            game.Weather,
            game.LastDayEnd,
            game.CreatedAt,
            game.SavedAt,
            game.Player,
            game.FarmPlot,
            game.ShippingBin,
            game.Residents,
            game.OpeningDone,
            game.PurchasedListings,
            resourceNodes: nodes);

    /// <summary>A Game on a fresh plot with the given energy (the two tools only) and the given ResourceNodes.</summary>
    public static GameState WithEnergyAndNodes(int energy, params ResourceNode[] nodes) =>
        WithNodes(TestGames.WithEnergy(energy), nodes);

    /// <summary>Each node as "spawnId:kind:area:x:y:swingsTaken", sorted, to compare node lists.</summary>
    public static IReadOnlyList<string> Describe(IEnumerable<ResourceNode> nodes) =>
        nodes.Select(node => $"{node.SpawnId}:{node.Kind}:{node.Area}:{node.Position.X}:{node.Position.Y}:{node.SwingsTaken}")
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>The nodes a spawn list places, as <see cref="Describe"/> writes them: one per atStart spawn, no swings.</summary>
    public static IReadOnlyList<string> DescribeSpawns(IEnumerable<NodeSpawn> spawns) =>
        spawns.Select(spawn => $"{spawn.Id}:{spawn.Kind}:{spawn.Area}:{spawn.Position.X}:{spawn.Position.Y}:0")
            .Order(StringComparer.Ordinal)
            .ToList();
}
