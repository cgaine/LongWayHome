namespace LongWayHome.Domain.Farming;

/// <summary>The 12 x 9 Farm plot: exactly one Tile per grid position (BR-T1).</summary>
public sealed class FarmPlot
{
    public const int Width = 12;

    public const int Height = 9;

    public FarmPlot() => throw new NotImplementedException();

    /// <summary>All Tiles in row-major order (Y then X).</summary>
    public IReadOnlyList<Tile> Tiles => throw new NotImplementedException();

    public Tile this[GridPosition position] => throw new NotImplementedException();
}
