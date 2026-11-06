using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Games;

/// <summary>
/// The Game aggregate root: one save's whole state — calendar (BR-G1), clock, weather, the Player and the Farm plot.
/// </summary>
public sealed class Game
{
    public Game(
        Guid id,
        GameDate date,
        int clockMinutes,
        Weather weather,
        DayEndReason? lastDayEnd,
        DateTimeOffset createdAt,
        DateTimeOffset savedAt,
        Player player,
        FarmPlot farmPlot)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(farmPlot);

        Id = id;
        Date = date;
        ClockMinutes = clockMinutes;
        Weather = weather;
        LastDayEnd = lastDayEnd;
        CreatedAt = createdAt;
        SavedAt = savedAt;
        Player = player;
        FarmPlot = farmPlot;
    }

    public Guid Id { get; }

    public GameDate Date { get; }

    public int ClockMinutes { get; }

    public Weather Weather { get; }

    public DayEndReason? LastDayEnd { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset SavedAt { get; }

    public Player Player { get; }

    public FarmPlot FarmPlot { get; }

    /// <summary>Why tilling a Tile would be refused, or null when it is allowed: AlreadyTilled, then TooTired (BR-T2, BR-P2).</summary>
    public Rejection? TillRejection(GridPosition position, int toolEnergyCost)
    {
        // BR-T2
        if (FarmPlot[position].State != TileState.Grass)
        {
            return Rejection.AlreadyTilled;
        }

        // BR-P2
        return Player.Energy < toolEnergyCost ? Rejection.TooTired : null;
    }

    /// <summary>Tills a Tile: AlreadyTilled, then TooTired, else tills and spends the energy (BR-T2, BR-P2).</summary>
    public ActionResult Till(GridPosition position, int toolEnergyCost)
    {
        // BR-P2: a rejected till spends nothing.
        if (TillRejection(position, toolEnergyCost) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        Player.TrySpendEnergy(toolEnergyCost);
        return FarmPlot[position].Till();
    }

    /// <summary>Why planting would be refused, or null: NotTilled, then TileOccupied, then NoSeed (BR-T3).</summary>
    public Rejection? PlantRejection(GridPosition position, CropTypeId cropTypeId)
    {
        // BR-T3
        var tile = FarmPlot[position];
        if (tile.State != TileState.Tilled)
        {
            return Rejection.NotTilled;
        }

        if (tile.Planting is not null)
        {
            return Rejection.TileOccupied;
        }

        return Player.Inventory.Quantity(ItemKind.Seed, cropTypeId) < 1 ? Rejection.NoSeed : null;
    }

    /// <summary>Plants one Seed on a Tile (BR-T3). Costs no energy (BR-P2).</summary>
    public ActionResult Plant(GridPosition position, CropTypeId cropTypeId)
    {
        if (PlantRejection(position, cropTypeId) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        // Both steps are known to succeed now: the Tile is tilled and empty, and the seed is held.
        // BR-P7: Remove drops the stack when it reaches 0.
        Player.Inventory.Remove(ItemKind.Seed, cropTypeId, 1);
        return FarmPlot[position].Plant(new Planting(cropTypeId, Date, 0));
    }

    /// <summary>Why harvesting would be refused, or null: NoPlanting, then NotRipe (BR-T6).</summary>
    public Rejection? HarvestRejection(GridPosition position, CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(cropType);

        // BR-T6
        if (FarmPlot[position].Planting is not { } planting)
        {
            return Rejection.NoPlanting;
        }

        return planting.StatusFor(cropType) == PlantingStatus.Ripe ? null : Rejection.NotRipe;
    }

    /// <summary>Harvests a ripe Planting into one Crop (BR-T6). Costs no energy (BR-P2).</summary>
    public ActionResult Harvest(GridPosition position, CropType cropType)
    {
        if (HarvestRejection(position, cropType) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        // BR-T6: the Planting goes and one Crop of its type is added; BR-P7 merges it into one stack.
        var result = FarmPlot[position].Harvest(cropType);
        Player.Inventory.Add(ItemKind.Crop, cropType.Id, 1);
        return result;
    }
}
