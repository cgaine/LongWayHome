using System.Text.Json.Serialization;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>The Farm plot with all of its Tiles (BR-T1: 72 x 36, so 2592).</summary>
internal sealed record FarmPlotDto
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required IReadOnlyList<TileDto> Tiles { get; init; }

    /// <summary>The fences and chests on the plot (#164). Optional on load: missing is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PlacedObjectDto?>? PlacedObjects { get; init; }
}
