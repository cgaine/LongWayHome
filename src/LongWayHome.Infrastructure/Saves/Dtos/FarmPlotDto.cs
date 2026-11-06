namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>The Farm plot with all of its Tiles (BR-T1: 12 x 9, so 108).</summary>
internal sealed record FarmPlotDto
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required IReadOnlyList<TileDto> Tiles { get; init; }
}
