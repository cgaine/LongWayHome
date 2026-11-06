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

    public int ClockMinutes { get; private set; }

    /// <summary>The Day's weather, rolled in the Night before it and unchanged during the Day (BR-G4).</summary>
    public Weather Weather { get; private set; }

    public DayEndReason? LastDayEnd { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset SavedAt { get; }

    public Player Player { get; }

    public FarmPlot FarmPlot { get; }

    /// <summary>Adds game minutes to the clock, never past the day end (BR-G2). Negative minutes throw.</summary>
    public void AdvanceClock(int minutes, int dayEndMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutes);

        // BR-G2: the clock stops at the day end. Long arithmetic so a huge step cannot overflow past it.
        if (ClockMinutes < dayEndMinutes)
        {
            ClockMinutes = (int)Math.Min((long)ClockMinutes + minutes, dayEndMinutes);
        }
    }

    /// <summary>
    /// The Night's growth step (BR-T4): each Planting grows on its Tile by its Crop type's data. A Planting whose Crop
    /// type has no data row is left unchanged (BR-T5: never removed).
    /// </summary>
    public void GrowPlantings(IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        foreach (var tile in FarmPlot.Tiles)
        {
            if (tile.Planting is { } planting && cropTypes.TryGetValue(planting.CropTypeId, out var cropType))
            {
                tile.GrowOvernight(cropType);
            }
        }
    }

    /// <summary>The Night's watering reset (BR-T7): every Tile is dry. Tile states are untouched (BR-T8).</summary>
    public void DryAllTiles()
    {
        foreach (var tile in FarmPlot.Tiles)
        {
            tile.Dry();
        }
    }

    /// <summary>Sets the Day's weather; only the Night calls this (BR-G4).</summary>
    public void SetWeather(Weather weather) => Weather = weather;

    /// <summary>The Night's rain step (BR-T7): every tilled Tile is watered; grass stays dry.</summary>
    public void WaterAllTilledTiles()
    {
        foreach (var tile in FarmPlot.Tiles)
        {
            tile.Soak();
        }
    }

    /// <summary>
    /// Starts the next Day: records why the last one ended, sets energy (BR-P1, BR-P3) and the clock. Energy outside
    /// 0–150 throws and changes nothing.
    /// </summary>
    public void StartNextDay(DayEndReason reason, int energy, int dayStartMinutes)
    {
        // BR-P1: set energy first so an out-of-range value leaves the Game unchanged.
        Player.StartDay(energy);
        LastDayEnd = reason;
        ClockMinutes = dayStartMinutes;
    }

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

    /// <summary>
    /// Tills a Tile: AlreadyTilled, then TooTired, else tills and spends the energy (BR-T2, BR-P2). On a rain Day the
    /// new tilled Tile is watered at once (BR-T7).
    /// </summary>
    public ActionResult Till(GridPosition position, int toolEnergyCost)
    {
        // BR-P2: a rejected till spends nothing.
        if (TillRejection(position, toolEnergyCost) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        Player.TrySpendEnergy(toolEnergyCost);
        var tile = FarmPlot[position];
        var result = tile.Till();

        // BR-T7: on a rain Day every tilled Tile is watered, including Tiles tilled during that Day.
        if (result.IsOk && Weather == Weather.Rain)
        {
            tile.Soak();
        }

        return result;
    }

    /// <summary>Why watering would be refused, or null: NotTilled, AlreadyWatered, CanEmpty, then TooTired (BR-T9, BR-W2, BR-P2).</summary>
    public Rejection? WaterRejection(GridPosition position, int toolEnergyCost)
    {
        // BR-T9
        var tile = FarmPlot[position];
        if (tile.State != TileState.Tilled)
        {
            return Rejection.NotTilled;
        }

        if (tile.Watered)
        {
            return Rejection.AlreadyWatered;
        }

        // BR-W2
        if (Player.WateringCan.Charges == 0)
        {
            return Rejection.CanEmpty;
        }

        // BR-P2
        return Player.Energy < toolEnergyCost ? Rejection.TooTired : null;
    }

    /// <summary>Waters a Tile, using one charge and the tool energy cost (BR-T9, BR-W2, BR-P2).</summary>
    public ActionResult Water(GridPosition position, int toolEnergyCost)
    {
        // BR-T9, BR-W2, BR-P2: a rejected watering uses no charge and spends nothing.
        if (WaterRejection(position, toolEnergyCost) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        // All three steps are known to succeed now: the Tile is tilled and dry, the can has a charge, energy suffices.
        FarmPlot[position].Water();
        Player.WateringCan.TryUseCharge();
        Player.TrySpendEnergy(toolEnergyCost);
        return ActionResult.Ok;
    }

    /// <summary>Fills the can to capacity at no energy cost (BR-W3).</summary>
    public ActionResult RefillCan()
    {
        // BR-W3: always allowed, even when full; BR-P2: refilling costs no energy.
        Player.WateringCan.Refill();
        return ActionResult.Ok;
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

        // BR-T6: one Crop of its type is added and the Planting goes; BR-P7 merges it into one stack. The Crop is added
        // first so a failing Add leaves the Planting in place.
        Player.Inventory.Add(ItemKind.Crop, cropType.Id, 1);
        return FarmPlot[position].Harvest(cropType);
    }
}
