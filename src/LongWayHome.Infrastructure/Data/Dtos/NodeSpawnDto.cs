using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record NodeSpawnDto
{
    public required string Id { get; init; }

    public required AreaId Area { get; init; }

    /// <summary>Only for spawn points in the mine; absent (null) everywhere else.</summary>
    public int? MineLevel { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required NodeKind Kind { get; init; }

    public required bool AtStart { get; init; }

    public required bool Respawns { get; init; }
}
