namespace LongWayHome.Domain.Farming;

/// <summary>The 72 x 36 Farm plot: exactly one Tile per grid position (BR-T1).</summary>
public sealed class FarmPlot
{
    public const int Width = 72;

    public const int Height = 36;

    /// <summary>Where the Phase 1 12 x 9 plot sits in the grown grid (BR-T1, #150).</summary>
    public static PlotBlock Phase1Block { get; } = new(30, 0, 12, 9);

    private readonly Tile[] _tiles;

    /// <summary>Creates a fresh Farm plot of 2592 grass, unwatered Tiles.</summary>
    public FarmPlot()
    {
        // BR-T1: exactly one Tile per grid position, stored row-major (Y then X).
        _tiles = new Tile[Width * Height];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                _tiles[IndexOf(x, y)] = new Tile(new GridPosition(x, y));
            }
        }
    }

    /// <summary>Restores a Farm plot from exactly one Tile per grid position (BR-T1).</summary>
    public FarmPlot(IEnumerable<Tile> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);

        // BR-T1: every position of the plot exactly once. GridPosition already keeps each one inside the plot.
        _tiles = new Tile[Width * Height];
        var count = 0;
        foreach (var tile in tiles)
        {
            ArgumentNullException.ThrowIfNull(tile, nameof(tiles));
            var (x, y) = (tile.Position.X, tile.Position.Y);
            if (_tiles[IndexOf(x, y)] is not null)
            {
                throw new ArgumentException($"More than one Tile at ({x}, {y}).", nameof(tiles));
            }

            _tiles[IndexOf(x, y)] = tile;
            count++;
        }

        if (count != _tiles.Length)
        {
            throw new ArgumentException($"A Farm plot needs exactly {_tiles.Length} Tiles, got {count}.", nameof(tiles));
        }
    }

    /// <summary>All Tiles in row-major order (Y then X).</summary>
    public IReadOnlyList<Tile> Tiles => Array.AsReadOnly(_tiles);

    public Tile this[GridPosition position] => _tiles[IndexOf(position.X, position.Y)];

    private static int IndexOf(int x, int y) => (y * Width) + x;
}
