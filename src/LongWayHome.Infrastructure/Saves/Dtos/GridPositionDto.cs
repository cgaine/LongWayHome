namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record GridPositionDto
{
    public required int X { get; init; }

    public required int Y { get; init; }
}
