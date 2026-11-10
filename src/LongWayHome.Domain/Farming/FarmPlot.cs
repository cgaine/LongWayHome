namespace LongWayHome.Domain.Farming;

/// <summary>The 72 x 36 Farm plot: exactly one Tile per grid position (BR-T1).</summary>
public sealed class FarmPlot
{
    public const int Width = 72;

    public const int Height = 36;

    /// <summary>Where the Phase 1 12 x 9 plot sits in the grown grid (BR-T1, #150).</summary>
    public static PlotBlock Phase1Block { get; } = new(30, 0, 12, 9);

    private readonly Tile[] _tiles;

    // BR-F1: at most one Placed object per Tile.
    private readonly Dictionary<GridPosition, PlacedObject> _placedByTile = [];

    private readonly List<PlacedObject> _placed = [];

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

    /// <summary>
    /// Restores a Farm plot from exactly one Tile per grid position (BR-T1) and its Placed objects, none by default:
    /// each inside the plot, on an untilled Tile, at most one per Tile (BR-F1).
    /// </summary>
    public FarmPlot(IEnumerable<Tile> tiles, IEnumerable<PlacedObject>? placedObjects = null)
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

        foreach (var placed in placedObjects ?? [])
        {
            ArgumentNullException.ThrowIfNull(placed, nameof(placedObjects));
            var position = placed.Position;

            // BR-F1: on the plot, on an untilled Tile, one per Tile.
            if (!Contains(position))
            {
                throw new ArgumentException($"A Placed object at {position} is off the Farm plot.", nameof(placedObjects));
            }

            if (this[position].State != TileState.Grass)
            {
                throw new ArgumentException(
                    $"A Placed object at {position} is on a tilled Tile (BR-F1).", nameof(placedObjects));
            }

            if (_placedByTile.ContainsKey(position))
            {
                throw new ArgumentException($"More than one Placed object at {position} (BR-F1).", nameof(placedObjects));
            }

            Put(placed);
        }
    }

    /// <summary>All Tiles in row-major order (Y then X).</summary>
    public IReadOnlyList<Tile> Tiles => Array.AsReadOnly(_tiles);

    /// <summary>
    /// The Tile at <paramref name="position"/>; a position off the 72 x 36 plot (one on another Area's grid, #154)
    /// throws <see cref="ArgumentOutOfRangeException"/> (BR-T1).
    /// </summary>
    public Tile this[GridPosition position] =>
        Contains(position)
            ? _tiles[IndexOf(position.X, position.Y)]
            : throw new ArgumentOutOfRangeException(
                nameof(position), position, $"The position is off the {Width} x {Height} Farm plot (BR-T1).");

    /// <summary>The fences and chests on the plot, at most one per Tile (BR-F1).</summary>
    public IReadOnlyList<PlacedObject> PlacedObjects => _placed.AsReadOnly();

    /// <summary>The Placed object on <paramref name="position"/>, or null.</summary>
    public PlacedObject? PlacedAt(GridPosition position) => _placedByTile.GetValueOrDefault(position);

    /// <summary>Whether <paramref name="position"/> lies on the 72 x 36 plot rather than another Area's grid (#154).</summary>
    public static bool Contains(GridPosition position) => position.X < Width && position.Y < Height;

    /// <summary>Puts a Placed object on its Tile; the caller has checked the Tile is free (BR-F1).</summary>
    internal void Put(PlacedObject placed)
    {
        _placedByTile.Add(placed.Position, placed);
        _placed.Add(placed);
    }

    /// <summary>Takes the Placed object off its Tile (BR-F2).</summary>
    internal void Remove(PlacedObject placed)
    {
        _placedByTile.Remove(placed.Position);
        _placed.Remove(placed);
    }

    private static int IndexOf(int x, int y) => (y * Width) + x;
}
