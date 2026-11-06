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
        // BR-T3: a Planting only ever sits on tilled soil.
        if (planting is not null && state != TileState.Tilled)
        {
            throw new ArgumentException("A Planting can only be on a tilled Tile.", nameof(planting));
        }

        Position = position;
        State = state;
        Watered = watered;
        Planting = planting;
    }

    public GridPosition Position { get; }

    public TileState State { get; private set; }

    public bool Watered { get; private set; }

    /// <summary>The Planting on this Tile, if any; at most one, and only on a tilled Tile.</summary>
    public Planting? Planting { get; private set; }

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

    /// <summary>Waters a tilled, unwatered Tile (BR-T9); the Game checks first.</summary>
    public void Water()
    {
        // BR-T9: only tilled soil can be watered; the Game refuses the other cases as results first.
        if (State != TileState.Tilled || Watered)
        {
            throw new InvalidOperationException("Only a tilled Tile that is not already watered can be watered.");
        }

        Watered = true;
    }

    /// <summary>Puts a Planting on the Tile: NotTilled, then TileOccupied (BR-T3).</summary>
    public ActionResult Plant(Planting planting)
    {
        ArgumentNullException.ThrowIfNull(planting);

        // BR-T3
        if (State != TileState.Tilled)
        {
            return ActionResult.Rejected(Rejection.NotTilled);
        }

        if (Planting is not null)
        {
            return ActionResult.Rejected(Rejection.TileOccupied);
        }

        Planting = planting;
        return ActionResult.Ok;
    }

    /// <summary>Harvests a ripe Planting: NoPlanting, then NotRipe. Leaves State and Watered unchanged (BR-T6).</summary>
    public ActionResult Harvest(CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(cropType);

        // BR-T6: only a Ripe Planting is harvested. BR-T5: nothing else removes a Planting.
        if (Planting is null)
        {
            return ActionResult.Rejected(Rejection.NoPlanting);
        }

        if (Planting.StatusFor(cropType) != PlantingStatus.Ripe)
        {
            return ActionResult.Rejected(Rejection.NotRipe);
        }

        // BR-T6, BR-T8: State and Watered stay as they were.
        Planting = null;
        return ActionResult.Ok;
    }
}
