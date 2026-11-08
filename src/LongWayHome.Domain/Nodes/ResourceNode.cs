using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Domain.Nodes;

/// <summary>
/// A tree, stump or rock standing at a spawn point (child of Game). Carries the spawn point's area and position,
/// copied from its <see cref="NodeSpawn"/>, so the Game can find the node on a Tile without the reference data.
/// </summary>
public sealed class ResourceNode
{
    public ResourceNode(string spawnId, NodeKind kind, AreaId area, GridPosition position, int swingsTaken = 0, bool? respawns = null)
    {
        // BR-N5: stumps and debris never grow back, whatever the spawn point says; every other kind does unless its
        // spawn point says otherwise.
        Respawns = (respawns ?? true) && kind is not (NodeKind.Stump or NodeKind.Debris);
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

    /// <summary>Whether this node grows back once cleared (BR-N4, BR-N5), copied from its spawn point.</summary>
    public bool Respawns { get; }

    /// <summary>The swings taken on this node since the last Night (BR-N2); changes only through the Game.</summary>
    public int SwingsTaken { get; internal set; }

    /// <summary>A node with no swings taken at the spawn point.</summary>
    public static ResourceNode FromSpawn(NodeSpawn spawn)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        return new ResourceNode(spawn.Id, spawn.Kind, spawn.Area, spawn.Position, respawns: spawn.Respawns);
    }
}
