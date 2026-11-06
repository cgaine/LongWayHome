namespace LongWayHome.Domain.Farming;

/// <summary>One Tile of the Farm plot. Starts as <see cref="TileState.Grass"/> and not watered.</summary>
public sealed class Tile
{
    public Tile(GridPosition position) => throw new NotImplementedException();

    public GridPosition Position { get; }

    public TileState State { get; }

    public bool Watered { get; }
}
