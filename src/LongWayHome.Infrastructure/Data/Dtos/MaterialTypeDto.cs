using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record MaterialTypeDto
{
    public required MaterialId Id { get; init; }

    public required string Name { get; init; }

    public required int MaxStack { get; init; }
}
