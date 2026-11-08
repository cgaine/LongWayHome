using LongWayHome.Domain.Nodes;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>
/// One ResourceNode in the save (version 2, #151): <c>{ "spawnId", "kind", "swingsTaken" }</c>. Its area and Tile
/// come from the spawn point in <c>nodes.json</c> on load.
/// </summary>
internal sealed record ResourceNodeDto
{
    public required string SpawnId { get; init; }

    public required NodeKind Kind { get; init; }

    public required int SwingsTaken { get; init; }
}
