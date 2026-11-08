using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>
/// Plans the batched drawing of the Farm plot's untouched grass (#150, REQ-14): every grass Tile grouped by its
/// <see cref="GrassTileArt.PickFor"/> layout, so the view draws one batch per layout instead of one node per Tile.
/// Non-grass Tiles are left out; they get their own view.
/// </summary>
public static class GrassBatches
{
    /// <summary>Every grass Tile's position and quarter turns, keyed by its <see cref="GrassTileArt"/> layout.</summary>
    public static IReadOnlyDictionary<int, IReadOnlyList<(GridPosition Position, int QuarterTurns)>> ByLayout(FarmPlot plot) =>
        throw new NotImplementedException();

    /// <summary>The positions of every grass Tile, in the plot's row-major order.</summary>
    public static IReadOnlyList<GridPosition> GrassPositions(FarmPlot plot) => throw new NotImplementedException();
}
