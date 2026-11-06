using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;

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
        FarmPlot farmPlot,
        ShippingBin? shippingBin = null)
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
        ShippingBin = shippingBin ?? new ShippingBin();
    }

    public Guid Id { get; }

    /// <summary>Today's date; only the Night moves it on (BR-G1).</summary>
    public GameDate Date { get; private set; }

    public int ClockMinutes { get; private set; }

    /// <summary>The Day's weather, rolled in the Night before it and unchanged during the Day (BR-G4).</summary>
    public Weather Weather { get; private set; }

    public DayEndReason? LastDayEnd { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset SavedAt { get; }

    public Player Player { get; }

    public FarmPlot FarmPlot { get; }

    /// <summary>The crops shipped today, paid for in the Night (BR-S1, BR-S2).</summary>
    public ShippingBin ShippingBin { get; }

    /// <summary>Records when the Game was saved (BR-G5).</summary>
    public void MarkSaved(DateTimeOffset at) => throw new NotImplementedException();

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

    /// <summary>The Night's date step (BR-G1): moves the Game to the next day.</summary>
    public void AdvanceDate() => Date = Date.Next();

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

    /// <summary>
    /// Why planting would be refused, or null: NotTilled, then TileOccupied, then NoSeed, then OutOfSeason when the
    /// Crop type's Season is not today's (BR-T3, BR-C1).
    /// </summary>
    public Rejection? PlantRejection(GridPosition position, CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(cropType);
        return PlantRejection(position, cropType.Id, cropType.Season);
    }

    /// <summary>
    /// Why planting a Seed would be refused, or null, in the order of
    /// <see cref="PlantRejection(GridPosition, CropType)"/>. A null <paramref name="season"/> is a Crop type with no
    /// data row: it has no Season to be planted in, so a plant that passes the other checks is OutOfSeason.
    /// </summary>
    public Rejection? PlantRejection(GridPosition position, CropTypeId cropTypeId, Season? season)
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

        if (Player.Inventory.Quantity(ItemKind.Seed, cropTypeId) < 1)
        {
            return Rejection.NoSeed;
        }

        // BR-T3, BR-C1: a Seed plants only in its Crop type's Season.
        return season == Date.Season ? null : Rejection.OutOfSeason;
    }

    /// <summary>Plants one Seed on a Tile (BR-T3). Costs no energy (BR-P2).</summary>
    public ActionResult Plant(GridPosition position, CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(cropType);
        var cropTypeId = cropType.Id;
        if (PlantRejection(position, cropType) is { } rejection)
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

    /// <summary>
    /// Moves every Crop stack into the Shipping Bin (BR-S1); NothingToShip when there is none. Costs no energy (BR-P2).
    /// </summary>
    public ActionResult ShipAll()
    {
        // BR-S1: only Crops ship; with none held nothing changes.
        var crops = Player.Inventory.Stacks.Where(stack => stack.Kind == ItemKind.Crop).ToList();
        if (crops.Count == 0)
        {
            return ActionResult.Rejected(Rejection.NothingToShip);
        }

        // BR-S1: every merged quantity is checked before anything moves, so an overflowing stack ships nothing.
        var inBin = ShippingBin.Items.ToDictionary(item => item.CropTypeId, item => item.Quantity);
        foreach (var stack in crops)
        {
            _ = checked(inBin.GetValueOrDefault(stack.CropTypeId) + stack.Quantity);
        }

        // BR-P2: shipping spends no energy; gold only changes in the Night (BR-S2).
        foreach (var stack in crops)
        {
            ShippingBin.Add(stack.CropTypeId, stack.Quantity);
            Player.Inventory.Remove(ItemKind.Crop, stack.CropTypeId, stack.Quantity);
        }

        return ActionResult.Ok;
    }

    /// <summary>
    /// The Night's payout (BR-S2): gold rises by quantity × sellPrice of every bin item, then the bin empties. An item
    /// whose Crop type has no data row pays 0 and is still removed. Returns the amount paid. An overflowing total
    /// throws and changes nothing (BR-P5).
    /// </summary>
    public int PayOutShippingBin(IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        var total = ShippingPayout(cropTypes);

        // The gold is earned before the bin empties, so a failing EarnGold leaves the bin to pay another Night.
        Player.EarnGold(total);
        ShippingBin.Empty();
        return total;
    }

    /// <summary>
    /// What the Night's payout would pay (BR-S2), changing nothing: Σ quantity × sellPrice over the bin's items, where
    /// an item with no data row pays 0. Throws on overflow, or when the Player's gold plus the total would overflow
    /// (BR-P5), so the Night can check it before any step changes the Game.
    /// </summary>
    public int ShippingPayout(IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        // BR-S2: each Crop type is looked up by key; a missing row pays 0.
        var total = 0;
        foreach (var item in ShippingBin.Items)
        {
            if (cropTypes.TryGetValue(item.CropTypeId, out var cropType))
            {
                total = checked(total + (item.Quantity * cropType.SellPrice.Value));
            }
        }

        // BR-P5: the gold the payout would leave must fit too.
        _ = checked(Player.Gold.Value + total);
        return total;
    }
}
