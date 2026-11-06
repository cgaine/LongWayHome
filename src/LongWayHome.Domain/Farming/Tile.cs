namespace LongWayHome.Domain.Farming;

/// <summary>One Tile of the Farm plot (BR-T1). Starts as <see cref="TileState.Grass"/> and not watered.</summary>
public sealed class Tile
{
    public Tile(GridPosition position)
    {
        Position = position;
        State = TileState.Grass;
        Watered = false;
    }

    public GridPosition Position { get; }

    public TileState State { get; }

    public bool Watered { get; }
}
