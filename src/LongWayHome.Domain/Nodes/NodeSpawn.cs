using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Domain.Nodes;

/// <summary>
/// Reference data for one node spawn point (<c>nodes.json</c>): a <see cref="NodeKind"/> at a grid position in an
/// area, whether a node stands there at the start (BR-N7) and whether it grows back (BR-N5).
/// </summary>
public sealed record NodeSpawn(
    string Id,
    AreaId Area,
    int? MineLevel,
    GridPosition Position,
    NodeKind Kind,
    bool AtStart,
    bool Respawns);
