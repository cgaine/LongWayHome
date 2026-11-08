using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Residents;
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
        ShippingBin? shippingBin = null,
        IReadOnlyList<Resident>? residents = null)
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
        DroppedItems = _droppedItems.AsReadOnly();
        Residents = ValidResidents(residents ?? DefaultResidents());
    }

    public Guid Id { get; }

    /// <summary>Today's date; only the Night moves it on (BR-G1).</summary>
    public GameDate Date { get; private set; }

    public int ClockMinutes { get; private set; }

    /// <summary>The Day's weather, rolled in the Night before it and unchanged during the Day (BR-G4).</summary>
    public Weather Weather { get; private set; }

    public DayEndReason? LastDayEnd { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    /// <summary>When the Game was last saved; the Night's save step sets it (BR-G5).</summary>
    public DateTimeOffset SavedAt { get; private set; }

    public Player Player { get; }

    public FarmPlot FarmPlot { get; }

    /// <summary>The crops shipped today, paid for in the Night (BR-S1, BR-S2).</summary>
    public ShippingBin ShippingBin { get; }

    private readonly List<DroppedItem> _droppedItems = [];

    /// <summary>The items on the ground, oldest first (BR-K3, BR-K4). Never saved; a loaded Game has none.</summary>
    public IReadOnlyList<DroppedItem> DroppedItems { get; }

    /// <summary>One Resident per <see cref="ResidentId"/>, in enum order (BR-R1).</summary>
    public IReadOnlyList<Resident> Residents { get; }

    /// <summary>Whether <paramref name="id"/>'s Resident is present (BR-R1).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="ResidentId"/> member.</exception>
    public bool IsPresent(ResidentId id) =>
        Enum.IsDefined(id)
            ? Residents[(int)id].Present
            : throw new ArgumentOutOfRangeException(nameof(id), id, "Not a resident.");

    /// <summary>BR-R1: Maggie and Clyde are present from the start; the others arrive later.</summary>
    public static IReadOnlyList<Resident> DefaultResidents() =>
        Enum.GetValues<ResidentId>()
            .Select(id => new Resident(id, present: id is ResidentId.Maggie or ResidentId.Clyde)) // BR-R1
            .ToList()
            .AsReadOnly();

    /// <summary>One Resident per <see cref="ResidentId"/>, in enum order, or the list is refused.</summary>
    private static IReadOnlyList<Resident> ValidResidents(IReadOnlyList<Resident> residents)
    {
        var ids = Enum.GetValues<ResidentId>();
        if (residents.Count != ids.Length)
        {
            throw new ArgumentException(
                $"A Game has exactly one Resident per ResidentId ({ids.Length}), not {residents.Count}.",
                nameof(residents));
        }

        for (var i = 0; i < ids.Length; i++)
        {
            if (residents[i] is null || residents[i].Id != ids[i])
            {
                throw new ArgumentException(
                    $"Resident {i} must be {ids[i]}, one per ResidentId in enum order.", nameof(residents));
            }
        }

        return residents.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gains a non-tool item: what fits goes in the Backpack (BR-K3), the rest drops at <paramref name="feet"/> as
    /// DroppedItems of at most 99 each (BR-K2), each a new entry never merged with one already there. A tool throws
    /// (BR-K6) and a quantity below 1 throws, both changing nothing.
    /// </summary>
    public GainResult Gain(ItemRef item, int quantity, GridPosition feet)
    {
        // BR-K6: tools never go through Gain, so they can never drop.
        if (item.Kind == ItemKind.Tool)
        {
            throw new ArgumentException("A tool is never gained as a stack or dropped (BR-K6).", nameof(item));
        }

        // BR-K3: matching stacks below 99, then the lowest free Slots; Add validates the item and the quantity.
        var left = Player.Backpack.Add(item, quantity);
        var added = quantity - left;
        if (left == 0)
        {
            return new GainResult(added, []);
        }

        // BR-K3, BR-K2: what fits nowhere drops at the feet in chunks of at most 99, never merged.
        var dropped = new List<DroppedItem>();
        while (left > 0)
        {
            var chunk = Math.Min(Backpack.MaxStack, left);
            dropped.Add(new DroppedItem(AreaId.Farm, null, feet, item, chunk));
            left -= chunk;
        }

        _droppedItems.AddRange(dropped);
        return new GainResult(added, dropped.AsReadOnly());
    }

    /// <summary>
    /// Picks up the DroppedItems at <paramref name="position"/> in <paramref name="area"/> (the farm by default),
    /// oldest first, as much as fits (BR-K4): one that all fits is removed, one that partly fits keeps the rest. Items
    /// in other areas are never touched. Returns whether anything moved. Costs no energy (BR-P2).
    /// </summary>
    public bool PickUp(GridPosition position, AreaId area = AreaId.Farm)
    {
        var moved = false;

        // BR-K4: oldest first, each through BR-K3; a later item may still top up a matching stack.
        foreach (var dropped in _droppedItems.Where(item => item.Area == area && item.Position == position).ToList())
        {
            var left = Player.Backpack.Add(dropped.Item, dropped.Quantity);
            if (left == dropped.Quantity)
            {
                continue;
            }

            moved = true;
            if (left == 0)
            {
                _droppedItems.Remove(dropped);
            }
            else
            {
                dropped.Quantity = left;
            }
        }

        return moved;
    }

    /// <summary>
    /// Whether any DroppedItem lies at <paramref name="position"/> in <paramref name="area"/> (the farm by default).
    /// </summary>
    public bool HasDroppedItemsAt(GridPosition position, AreaId area = AreaId.Farm) =>
        _droppedItems.Exists(item => item.Area == area && item.Position == position);

    /// <summary>
    /// Whether the Player may enter <paramref name="area"/> (BR-A1). Derived, never stored: the farm, forest path, town
    /// and lakeshore are always open; across the river and the mine stay closed until their epics add their conditions.
    /// Nothing makes an open area close.
    /// </summary>
    public bool IsAreaOpen(AreaId area) =>
        // BR-A1: the four start areas are open from the start and never close. across_river (carpenter stage 2, #125)
        // and mine (BR-M1, #126) extend this with their conditions when they are built.
        area is AreaId.Farm or AreaId.ForestPath or AreaId.Town or AreaId.Lakeshore;

    /// <summary>The Night's dropped-items step (BR-K4): every DroppedItem is removed.</summary>
    public void ClearDroppedItems() => _droppedItems.Clear();

    /// <summary>Records when the Game was saved (BR-G5).</summary>
    public void MarkSaved(DateTimeOffset at) => SavedAt = at;

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

    /// <summary>Fills the can to capacity at no energy cost at any open Water source (BR-W3).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="WaterSource"/> member.</exception>
    public ActionResult RefillCan(WaterSource source)
    {
        // BR-A2: the pond is on the farm, the river along the meadow and through town, the lake at the lakeshore; each
        // is a Water source, and their areas are open from the start (BR-A1), so every source is open.
        switch (source)
        {
            case WaterSource.Pond:
            case WaterSource.River:
            case WaterSource.Lake:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, "Not a water source.");
        }

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
    public Rejection? PlantRejection(GridPosition position, CropTypeId cropTypeId, Season? season) =>
        PlantRejection(position, season, hasSeed: Player.Backpack.Quantity(ItemRef.Seed(cropTypeId)) >= 1);

    /// <summary>
    /// Why planting the Seed in one Backpack Slot would be refused, or null, in the order of
    /// <see cref="PlantRejection(GridPosition, CropType)"/>; NoSeed when that Slot does not hold this Crop type's Seed.
    /// A <paramref name="slotIndex"/> outside the Backpack throws.
    /// </summary>
    public Rejection? PlantFromSlotRejection(GridPosition position, CropType cropType, int slotIndex)
    {
        ArgumentNullException.ThrowIfNull(cropType);
        return PlantFromSlotRejection(position, cropType.Id, cropType.Season, slotIndex);
    }

    /// <summary>
    /// <see cref="PlantFromSlotRejection(GridPosition, CropType, int)"/> for a Crop type that may have no data row: a
    /// null <paramref name="season"/> has no Season to be planted in, so a plant that passes the other checks is
    /// OutOfSeason.
    /// </summary>
    public Rejection? PlantFromSlotRejection(GridPosition position, CropTypeId cropTypeId, Season? season, int slotIndex) =>
        PlantRejection(position, season, hasSeed: SlotHoldsSeed(slotIndex, cropTypeId));

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
        // BR-T3: planting uses one Seed; BR-K2: a Slot that reaches 0 is emptied.
        Player.Backpack.Remove(ItemRef.Seed(cropTypeId), 1);
        return FarmPlot[position].Plant(new Planting(cropTypeId, Date, 0));
    }

    /// <summary>
    /// Plants one Seed taken from one Backpack Slot (BR-T3), with the rejections of
    /// <see cref="PlantFromSlotRejection"/>. Costs no energy (BR-P2).
    /// </summary>
    public ActionResult PlantFromSlot(GridPosition position, CropType cropType, int slotIndex)
    {
        if (PlantFromSlotRejection(position, cropType, slotIndex) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        // BR-T3: planting uses one Seed, from this Slot only; BR-K2: a Slot that reaches 0 is emptied.
        Player.Backpack.RemoveFrom(slotIndex, 1);
        return FarmPlot[position].Plant(new Planting(cropType.Id, Date, 0));
    }

    /// <summary>
    /// Why harvesting would be refused, or null: NoPlanting, then NotRipe (BR-T6). A full Backpack never refuses a
    /// harvest: the Crop drops at the farmer's feet (BR-K3).
    /// </summary>
    public Rejection? HarvestRejection(GridPosition position, CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(cropType);

        // BR-T6
        if (FarmPlot[position].Planting is not { } planting)
        {
            return Rejection.NoPlanting;
        }

        return planting.StatusFor(cropType) != PlantingStatus.Ripe ? Rejection.NotRipe : null;
    }

    /// <summary>
    /// Harvests a ripe Planting into one Crop (BR-T6), gained at <paramref name="feet"/> (BR-K3): with no room it drops
    /// there, or on the harvested Tile when <paramref name="feet"/> is null. Costs no energy (BR-P2).
    /// </summary>
    public ActionResult Harvest(GridPosition position, CropType cropType, GridPosition? feet = null)
    {
        if (HarvestRejection(position, cropType) is { } rejection)
        {
            return ActionResult.Rejected(rejection);
        }

        // BR-T6: one Crop of its type is gained and the Planting goes; BR-K3 tops up a matching stack first and drops
        // what fits nowhere at the feet (the harvested Tile when none is given).
        Gain(ItemRef.Crop(cropType.Id), 1, feet ?? position);
        return FarmPlot[position].Harvest(cropType);
    }

    /// <summary>
    /// Moves every Crop in every Backpack Slot into the Shipping Bin, merged per Crop type (BR-S1); NothingToShip when
    /// there is none. Costs no energy (BR-P2).
    /// </summary>
    public ActionResult ShipAll()
    {
        // BR-S1, BR-K6: only Crops ship, never seeds or tools; with none held nothing changes.
        var crops = Player.Backpack.Totals(ItemKind.Crop);
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
            Player.Backpack.Remove(ItemRef.Crop(stack.CropTypeId), stack.Quantity);
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

    // BR-T3, BR-C1: the checks in order: tilled, empty, a Seed held, then its Crop type's Season.
    private Rejection? PlantRejection(GridPosition position, Season? season, bool hasSeed)
    {
        var tile = FarmPlot[position];
        if (tile.State != TileState.Tilled)
        {
            return Rejection.NotTilled;
        }

        if (tile.Planting is not null)
        {
            return Rejection.TileOccupied;
        }

        if (!hasSeed)
        {
            return Rejection.NoSeed;
        }

        // BR-T3, BR-C1: a Seed plants only in its Crop type's Season.
        return season == Date.Season ? null : Rejection.OutOfSeason;
    }

    private bool SlotHoldsSeed(int slotIndex, CropTypeId cropTypeId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, Player.Backpack.Capacity);
        return Player.Backpack.Slots[slotIndex].Item == ItemRef.Seed(cropTypeId);
    }
}
