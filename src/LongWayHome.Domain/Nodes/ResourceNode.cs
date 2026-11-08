using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Domain.Nodes;

/// <summary>
/// A tree, stump or rock standing at a spawn point (child of Game). Carries the spawn point's area and position,
/// copied from its <see cref="NodeSpawn"/>, so the Game can find the node on a Tile without the reference data.
/// </summary>
public sealed class ResourceNode
{
    public ResourceNode(string spawnId, NodeKind kind, AreaId area, GridPosition position, int swingsTaken = 0)
    {
        SpawnId = spawnId;
        Kind = kind;
        Area = area;
        Position = position;
        SwingsTaken = swingsTaken;
    }

    public string SpawnId { get; }

    public NodeKind Kind { get; }

    public AreaId Area { get; }

    public GridPosition Position { get; }

    /// <summary>Always 0 until the chopping feature.</summary>
    public int SwingsTaken { get; }

    /// <summary>A node with no swings taken at the spawn point.</summary>
    public static ResourceNode FromSpawn(NodeSpawn spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        return new ResourceNode(spawn.Id, spawn.Kind, spawn.Area, spawn.Position);
    }
}
