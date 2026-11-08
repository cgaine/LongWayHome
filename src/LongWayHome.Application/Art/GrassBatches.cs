using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>
/// Plans the batched drawing of the Farm plot's untouched grass (#150, REQ-14): every grass Tile grouped by its
/// <see cref="GrassTileArt.PickFor"/> layout, so the view draws one batch per layout instead of one node per Tile.
/// The native plants some grass Tiles carry are batched by kind too. Non-grass Tiles are left out; they get their own
/// view.
/// </summary>
public static class GrassBatches
{
    /// <summary>Every grass Tile's position and quarter turns, keyed by its <see cref="GrassTileArt"/> layout.</summary>
    public static IReadOnlyDictionary<int, IReadOnlyList<(GridPosition Position, int QuarterTurns)>> ByLayout(FarmPlot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);
        var batches = new SortedDictionary<int, List<(GridPosition Position, int QuarterTurns)>>();
        foreach (var position in GrassPositions(plot))
        {
            var (variant, turns) = GrassTileArt.PickFor(position);
            if (!batches.TryGetValue(variant, out var batch))
            {
                batch = [];
                batches.Add(variant, batch);
            }

            batch.Add((position, turns));
        }

        return batches.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<(GridPosition Position, int QuarterTurns)>)entry.Value.AsReadOnly());
    }

    /// <summary>
    /// The grass Tiles that carry a native plant (<see cref="TileDecoration.For"/>), keyed by its kind, in the plot's
    /// row-major order; a kind no grass Tile carries has no entry.
    /// </summary>
    public static IReadOnlyDictionary<NativePlantKind, IReadOnlyList<GridPosition>> ByNativePlant(FarmPlot plot) =>
        GrassPositions(plot)
            .Select(position => (Position: position, Kind: TileDecoration.For(position).NativePlant))
            .Where(entry => entry.Kind != NativePlantKind.None)
            .GroupBy(entry => entry.Kind)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<GridPosition>)group.Select(entry => entry.Position).ToList().AsReadOnly());

    /// <summary>The positions of every grass Tile, in the plot's row-major order.</summary>
    public static IReadOnlyList<GridPosition> GrassPositions(FarmPlot plot)
    {
        ArgumentNullException.ThrowIfNull(plot);
        return plot.Tiles.Where(tile => tile.State == TileState.Grass).Select(tile => tile.Position).ToList().AsReadOnly();
    }
}
