namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record NodesFileDto
{
    public required IReadOnlyList<NodeSpawnDto> Spawns { get; init; }
}
