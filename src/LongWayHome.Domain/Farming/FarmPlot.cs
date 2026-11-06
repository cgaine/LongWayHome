namespace LongWayHome.Domain.Farming;

/// <summary>The 12 x 9 Farm plot: exactly one Tile per grid position (BR-T1).</summary>
public sealed class FarmPlot
{
    public const int Width = 12;

    public const int Height = 9;

    private readonly Tile[] _tiles;

    /// <summary>Creates a fresh Farm plot of 108 grass, unwatered Tiles.</summary>
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

    /// <summary>All Tiles in row-major order (Y then X).</summary>
    public IReadOnlyList<Tile> Tiles => Array.AsReadOnly(_tiles);

    public Tile this[GridPosition position] => _tiles[IndexOf(position.X, position.Y)];

    private static int IndexOf(int x, int y) => (y * Width) + x;
}
