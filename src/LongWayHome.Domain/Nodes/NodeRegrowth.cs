using LongWayHome.Domain.Games;

namespace LongWayHome.Domain.Nodes;

/// <summary>
/// A cleared respawning spawn point waiting to grow back (child of Game, BR-N4): its spawn id and the date it was
/// cleared.
/// </summary>
public sealed record NodeRegrowth(string SpawnId, GameDate ClearedOn);
