using LongWayHome.Domain.Catalog;
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

    /// <summary>Restores a Tile; a Planting is allowed only on a tilled Tile.</summary>
    public Tile(GridPosition position, TileState state, bool watered, Planting? planting)
    {
        throw new NotImplementedException();
    }

    public GridPosition Position { get; }

    public TileState State { get; private set; }

    public bool Watered { get; }

    public Planting? Planting { get; }

    /// <summary>Tills a grass Tile (BR-T2); a Tile that is not grass returns AlreadyTilled. Never reverts (BR-T8).</summary>
    public ActionResult Till()
    {
        // BR-T2: only grass can be tilled. BR-T8: nothing sets a tilled Tile back to grass.
        if (State != TileState.Grass)
        {
            return ActionResult.Rejected(Rejection.AlreadyTilled);
        }

        State = TileState.Tilled;
        return ActionResult.Ok;
    }

    /// <summary>Puts a Planting on the Tile: NotTilled, then TileOccupied (BR-T3).</summary>
    public ActionResult Plant(Planting planting) => throw new NotImplementedException();

    /// <summary>Harvests a ripe Planting: NoPlanting, then NotRipe. Leaves State and Watered unchanged (BR-T6).</summary>
    public ActionResult Harvest(CropType cropType) => throw new NotImplementedException();
}
