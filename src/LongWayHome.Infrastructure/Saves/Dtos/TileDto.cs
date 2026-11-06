using LongWayHome.Domain.Farming;

namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record TileDto
{
    public required GridPositionDto Position { get; init; }

    public required TileState State { get; init; }

    public required bool Watered { get; init; }

    /// <summary>Written as null on a Tile with no Planting.</summary>
    public required PlantingDto? Planting { get; init; }
}
