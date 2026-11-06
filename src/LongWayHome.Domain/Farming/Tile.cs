using LongWayHome.Domain.Games;

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

    public TileState State { get; private set; }

    public bool Watered { get; }

    /// <summary>Tills a grass Tile (BR-T2); a Tile that is not grass returns AlreadyTilled. Never reverts (BR-T8).</summary>
    public ActionResult Till() => throw new NotImplementedException();
}
