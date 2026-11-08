using System.Collections.Immutable;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Nodes;
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
        IReadOnlyList<Resident>? residents = null,
        bool openingDone = true,
        IReadOnlySet<string>? purchasedListings = null,
        IReadOnlyList<ResourceNode>? resourceNodes = null,
        IReadOnlyList<NodeRegrowth>? nodeRegrowths = null)
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
        OpeningDone = openingDone;
        _purchasedListings = ValidPurchasedListings(purchasedListings);
        (_resourceNodes, _nodesByTile) = ValidResourceNodes(resourceNodes ?? []);
        ResourceNodes = _resourceNodes.AsReadOnly();
        _nodeRegrowths = ValidNodeRegrowths(nodeRegrowths ?? [], _resourceNodes);
        NodeRegrowths = _nodeRegrowths.AsReadOnly();
    }

    public Guid Id { get; }

    private ImmutableHashSet<string> _purchasedListings;

    /// <summary>The ids of the once-per-game Shop listings bought (BR-L5); changes only through RecordPurchase.</summary>
    public IReadOnlySet<string> PurchasedListings => _purchasedListings;

    /// <summary>
    /// Records a once-per-game listing as bought (BR-L5); recording it again changes nothing. A null or blank id
    /// throws.
    /// </summary>
    public void RecordPurchase(string listingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listingId);

        // BR-L5: "bought" is remembered for the rest of the Game; a set, so a second record changes nothing.
        _purchasedListings = _purchasedListings.Add(listingId);
    }

    /// <summary>A copy of the given ids (ordinal), refusing a null or blank one; none given is empty.</summary>
    private static ImmutableHashSet<string> ValidPurchasedListings(IReadOnlySet<string>? purchasedListings)
    {
        var ids = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var id in purchasedListings ?? ImmutableHashSet<string>.Empty)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A purchased listing id is never blank.", nameof(purchasedListings));
            }

            ids.Add(id);
        }

        return ids.ToImmutable();
    }

    /// <summary>
    /// Whether Maggie's Opening has finished (BR-I1). A new Game starts with it false; every Game saved before the
    /// Opening existed loads with it true.
    /// </summary>
    public bool OpeningDone { get; private set; }

    /// <summary>Marks the Opening done (BR-I1); calling it again changes nothing.</summary>
    public void CompleteOpening() => OpeningDone = true; // BR-I1: set once, never cleared

    /// <summary>
    /// Maggie hands over the starter Seeds (BR-P6). Refused OpeningOver once the Opening is done (BR-I1), and
    /// BackpackFull with nothing added when the stacks don't all fit (BR-K3: they top up and fill free Slots as
    /// <see cref="Backpack.Add"/> does). Costs no energy (BR-P2).
    /// </summary>
    public ActionResult GiveStarterSeeds(IReadOnlyList<StartingStack> seeds)
    {
        ArgumentNullException.ThrowIfNull(seeds);

        // BR-I1: the seeds are part of the Opening, so they are given only while it runs.
        if (OpeningDone)
        {
            return ActionResult.Rejected(Rejection.OpeningOver);
        }

        // BR-K3: check every stack fits first, on a copy, so a list that half fits adds nothing.
        var backpack = Player.Backpack;
        var trial = new Backpack(backpack.Capacity, backpack.HotbarRow, backpack.Slots);
        if (seeds.Any(stack => trial.Add(ItemRef.Seed(stack.CropTypeId), stack.Quantity) != 0))
        {
            return ActionResult.Rejected(Rejection.BackpackFull);
        }

        // BR-P6: the tuning's starting Seeds, from Maggie.
        foreach (var stack in seeds)
        {
            backpack.Add(ItemRef.Seed(stack.CropTypeId), stack.Quantity);
        }

        return ActionResult.Ok;
    }

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

    /// <summary>The trees, stumps and rocks standing in the world: at most one per spawn point and one per Tile.</summary>
    public IReadOnlyList<ResourceNode> ResourceNodes { get; }

    private readonly List<ResourceNode> _resourceNodes;

    private readonly Dictionary<(AreaId Area, GridPosition Position), ResourceNode> _nodesByTile;

    /// <summary>The node on <paramref name="position"/> in <paramref name="area"/>, or null.</summary>
    public ResourceNode? NodeAt(AreaId area, GridPosition position) =>
        _nodesByTile.GetValueOrDefault((area, position));

    /// <summary>At most one node per spawn point and one per (area, Tile), or the list is refused.</summary>
    private static (List<ResourceNode> Nodes, Dictionary<(AreaId, GridPosition), ResourceNode> ByTile)
        ValidResourceNodes(IReadOnlyList<ResourceNode> resourceNodes)
    {
        var spawnIds = new HashSet<string>(StringComparer.Ordinal);
        var byTile = new Dictionary<(AreaId, GridPosition), ResourceNode>();
        foreach (var node in resourceNodes)
        {
            if (node is null)
            {
                throw new ArgumentException("A Game's ResourceNodes can't contain null.", nameof(resourceNodes));
            }

            if (!spawnIds.Add(node.SpawnId))
            {
                throw new ArgumentException(
                    $"Two ResourceNodes share the spawn point '{node.SpawnId}'.", nameof(resourceNodes));
            }

            if (!byTile.TryAdd((node.Area, node.Position), node))
            {
                var other = byTile[(node.Area, node.Position)];
                throw new ArgumentException(
                    $"ResourceNodes '{other.SpawnId}' and '{node.SpawnId}' stand on the same Tile " +
                    $"({node.Position.X}, {node.Position.Y}) in {node.Area}.",
                    nameof(resourceNodes));
            }
        }

        return (resourceNodes.ToList(), byTile);
    }

    /// <summary>The cleared respawning spawn points waiting to grow back (BR-N4), at most one per spawn point.</summary>
    public IReadOnlyList<NodeRegrowth> NodeRegrowths { get; }

    private readonly List<NodeRegrowth> _nodeRegrowths;

    /// <summary>
    /// At most one NodeRegrowth per spawn point, none null and none for a spawn point whose node stands, or the list
    /// is refused.
    /// </summary>
    private static List<NodeRegrowth> ValidNodeRegrowths(
        IReadOnlyList<NodeRegrowth> nodeRegrowths, IReadOnlyList<ResourceNode> standing)
    {
        var spawnIds = new HashSet<string>(StringComparer.Ordinal);
        var standingIds = standing.Select(node => node.SpawnId).ToHashSet(StringComparer.Ordinal);
        foreach (var regrowth in nodeRegrowths)
        {
            if (regrowth?.SpawnId is null)
            {
                throw new ArgumentException(
                    "A Game's NodeRegrowths can't contain null or a null spawn id.", nameof(nodeRegrowths));
            }

            if (!spawnIds.Add(regrowth.SpawnId))
            {
                throw new ArgumentException(
                    $"Two NodeRegrowths share the spawn point '{regrowth.SpawnId}'.", nameof(nodeRegrowths));
            }

            if (standingIds.Contains(regrowth.SpawnId))
            {
                throw new ArgumentException(
                    $"The spawn point '{regrowth.SpawnId}' has a standing node and a NodeRegrowth.", nameof(nodeRegrowths));
            }
        }

        return nodeRegrowths.ToList();
    }

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
    /// Gains a non-tool item: what fits goes in the Backpack (BR-K3), the rest drops at <paramref name="feet"/> in
    /// <paramref name="area"/> (the farm by default) as DroppedItems of at most 99 each (BR-K2), each a new entry never
    /// merged with one already there. A tool throws (BR-K6) and a quantity below 1 throws, both changing nothing; so does
    /// a <paramref name="feet"/> off the Area's grid once anything drops.
    /// </summary>
    public GainResult Gain(ItemRef item, int quantity, GridPosition feet, AreaId area = AreaId.Farm)
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
            dropped.Add(new DroppedItem(area, null, feet, item, chunk));
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

    /// <summary>
    /// Why tilling a Tile would be refused, or null when it is allowed: AlreadyTilled, then NodeInTheWay, then
    /// TooTired (BR-T2, BR-P2).
    /// </summary>
    public Rejection? TillRejection(GridPosition position, int toolEnergyCost)
    {
        // BR-T2
        if (FarmPlot[position].State != TileState.Grass)
        {
            return Rejection.AlreadyTilled;
        }

        // BR-T2: a Tile with a Resource node on it can't be tilled.
        if (NodeAt(AreaId.Farm, position) is not null)
        {
            return Rejection.NodeInTheWay;
        }

        // BR-P2
        return Player.Energy < toolEnergyCost ? Rejection.TooTired : null;
    }

    /// <summary>
    /// Tills a Tile: AlreadyTilled, then NodeInTheWay, then TooTired, else tills and spends the energy (BR-T2, BR-P2). On a rain Day the
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

    /// <summary>
    /// Sells every carried Crop of one type to Maggie for quantity × sellPrice, paid at once (BR-L3), across every Slot
    /// of that type (BR-K2). NothingToSell when none are carried; NotSellable when the Crop type has no data row, so it
    /// stays. Only Crops are sold, never that type's Seeds or a tool (BR-L3, BR-K6). An overflowing total throws and
    /// changes nothing (BR-P5). Costs no energy and leaves the Shipping Bin alone (BR-P2).
    /// </summary>
    public ActionResult SellCrop(CropTypeId cropTypeId, IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        // BR-L3, BR-K6: only the Crop kind is counted, so Seeds of the same type and tools are never sold.
        var crop = ItemRef.Crop(cropTypeId);
        var quantity = Player.Backpack.Quantity(crop);
        if (quantity == 0)
        {
            return ActionResult.Rejected(Rejection.NothingToSell);
        }

        // BR-L3: with no data row there is no price, so Maggie can't buy it and it stays carried.
        if (!cropTypes.ContainsKey(cropTypeId))
        {
            return ActionResult.Rejected(Rejection.NotSellable);
        }

        // BR-P5: the total and the gold it leaves are checked before anything changes.
        var total = SellPayout(cropTypeId, cropTypes);

        // BR-L3, BR-P2: the Crops go and the gold is earned at once, with no energy spent and no Shipping Bin.
        Player.Backpack.Remove(crop, quantity);
        Player.EarnGold(total);
        return ActionResult.Ok;
    }

    /// <summary>
    /// Sells every carried Crop type that has a data row to Maggie, as one step (BR-L3); types with no data row stay.
    /// NothingToSell when there are none of those. The whole total is checked first: an overflow throws and changes
    /// nothing (BR-P5). Costs no energy and leaves the Shipping Bin alone (BR-P2).
    /// </summary>
    public ActionResult SellAllCrops(IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        // BR-L3, BR-K6: only Crops with a price are sold, never Seeds or tools.
        var sellable = SellableTotals(cropTypes);
        if (sellable.Count == 0)
        {
            return ActionResult.Rejected(Rejection.NothingToSell);
        }

        // BR-P5: the whole total and the gold it leaves are checked before any Slot changes.
        var total = SellAllTotal(sellable, cropTypes);

        // BR-L3, BR-K2, BR-P2: every sold Slot empties and the gold is earned at once, with no energy spent.
        foreach (var stack in sellable)
        {
            Player.Backpack.Remove(ItemRef.Crop(stack.CropTypeId), stack.Quantity);
        }

        Player.EarnGold(total);
        return ActionResult.Ok;
    }

    /// <summary>
    /// What <see cref="SellCrop"/> would pay (BR-L3), changing nothing: carried quantity × sellPrice, or 0 when none are
    /// carried or the type has no data row. Throws on overflow, or when the Player's gold plus the total would
    /// overflow (BR-P5).
    /// </summary>
    public int SellPayout(CropTypeId cropTypeId, IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        var total = 0;
        if (cropTypes.TryGetValue(cropTypeId, out var cropType))
        {
            total = checked(Player.Backpack.Quantity(ItemRef.Crop(cropTypeId)) * cropType.SellPrice.Value);
        }

        // BR-P5: the gold the sale would leave must fit too.
        _ = checked(Player.Gold.Value + total);
        return total;
    }

    /// <summary>
    /// What <see cref="SellAllCrops"/> would pay (BR-L3), changing nothing: Σ quantity × sellPrice over the carried Crop
    /// types with a data row. Throws on overflow, or when the Player's gold plus the total would overflow (BR-P5).
    /// </summary>
    public int SellAllPayout(IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        return SellAllTotal(SellableTotals(cropTypes), cropTypes);
    }

    // BR-L3, BR-P5: Σ quantity × sellPrice over the given sellable totals; throws when it, or the gold it leaves, overflows.
    private int SellAllTotal(
        IEnumerable<(CropTypeId CropTypeId, int Quantity)> sellable,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        var total = 0;
        foreach (var stack in sellable)
        {
            total = checked(total + (stack.Quantity * cropTypes[stack.CropTypeId].SellPrice.Value));
        }

        // BR-P5: the gold the sale would leave must fit too.
        _ = checked(Player.Gold.Value + total);
        return total;
    }

    // BR-L3: the carried Crop totals whose type has a data row, so a price; the rest can't be sold.
    private List<(CropTypeId CropTypeId, int Quantity)> SellableTotals(IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        Player.Backpack.Totals(ItemKind.Crop).Where(stack => cropTypes.ContainsKey(stack.CropTypeId)).ToList();

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

    /// <summary>
    /// Why a swing of <paramref name="tool"/> at <paramref name="tier"/> at the node on <paramref name="target"/> would
    /// be refused, or null: NothingToDo (BR-N1), then NeedsGrothAxe (BR-N6), then TooTired (BR-P2).
    /// </summary>
    public Rejection? SwingRejection(AreaId area, GridPosition target, ToolId tool, ToolTier tier, int toolEnergyCost)
    {
        // BR-N1: the axe works on trees and stumps, the pickaxe on rocks, ore deposits and debris; nothing else swings.
        if (NodeAt(area, target) is not { } node || !Works(tool, node.Kind))
        {
            return Rejection.NothingToDo;
        }

        // BR-N6: a stump can be cleared only with a Groth axe.
        if (node.Kind == NodeKind.Stump && tier != ToolTier.Groth)
        {
            return Rejection.NeedsGrothAxe;
        }

        // BR-P2
        return Player.Energy < toolEnergyCost ? Rejection.TooTired : null;
    }

    /// <summary>
    /// Swings the axe or pickaxe at the node on <paramref name="target"/>: spends the energy (BR-P2) and counts the
    /// swing; on the tier's count (BR-N2) the node is removed and its yield gained at <paramref name="feet"/> in
    /// <paramref name="area"/> (BR-N3,
    /// BR-K3), one <see cref="GainResult"/> per yield stack in yield order. A refused swing changes nothing. Tuning
    /// with no count of at least 1 for <paramref name="tier"/>, or a yield stack below 1, throws and changes nothing.
    /// </summary>
    public SwingResult Swing(
        AreaId area,
        GridPosition target,
        ToolId tool,
        ToolTier tier,
        int toolEnergyCost,
        IReadOnlyDictionary<ToolTier, int> swingsToClear,
        IReadOnlyDictionary<NodeKind, IReadOnlyList<MaterialStack>> nodeYields,
        GridPosition feet)
    {
        ArgumentNullException.ThrowIfNull(swingsToClear);
        ArgumentNullException.ThrowIfNull(nodeYields);

        // BR-N1, BR-N6, BR-P2: a refused swing changes nothing and costs nothing.
        if (SwingRejection(area, target, tool, tier, toolEnergyCost) is { } rejection)
        {
            return new SwingResult(ActionResult.Rejected(rejection), false, []);
        }

        var node = NodeAt(area, target)!;

        // Checked before anything changes, so bad tuning never leaves a half-done swing.
        if (!swingsToClear.TryGetValue(tier, out var count) || count < 1)
        {
            throw new ArgumentException(
                $"The tuning has no swing count of at least 1 for the {tier} tier.", nameof(swingsToClear));
        }

        var yields = nodeYields.GetValueOrDefault(node.Kind) ?? [];
        if (yields.Any(stack => stack is null || stack.Quantity < 1))
        {
            throw new ArgumentException($"The {node.Kind} yield has a stack below 1.", nameof(nodeYields));
        }

        // BR-P2, BR-N2: each swing spends the energy and counts on the node.
        Player.TrySpendEnergy(toolEnergyCost);
        node.SwingsTaken++;
        if (node.SwingsTaken < count)
        {
            return new SwingResult(ActionResult.Ok, false, []);
        }

        // BR-N3: the node is gone and its Tile free; the yield goes through BR-K3 (tier bonus 0 until #127).
        _resourceNodes.Remove(node);
        _nodesByTile.Remove((node.Area, node.Position));

        // BR-N4: a respawning spawn point waits to grow back from today; BR-N5: a stump or debris records nothing.
        if (node.Respawns)
        {
            _nodeRegrowths.Add(new NodeRegrowth(node.SpawnId, Date));
        }

        var gained = yields
            .Select(stack => Gain(ItemRef.Material(stack.Material), stack.Quantity, feet, area))
            .ToList()
            .AsReadOnly();
        return new SwingResult(ActionResult.Ok, true, gained);
    }

    /// <summary>The Night's node-regrowth slot (BR-N2): every node's SwingsTaken goes back to 0.</summary>
    public void ResetSwings()
    {
        foreach (var node in _resourceNodes)
        {
            node.SwingsTaken = 0;
        }
    }

    /// <summary>
    /// The Night's regrowth step (BR-N4), run before the date step, so the Night in progress counts: a NodeRegrowth
    /// grows back once at least <paramref name="nights"/> Nights have passed since it was cleared and its spawn point
    /// is free. On the farm a spawn point is free when no Tile within 3 tiles of it (its own included) is tilled; off
    /// the farm only the wait applies. A grown node is fresh (0 swings) and its NodeRegrowth goes; a blocked one keeps
    /// waiting. A NodeRegrowth whose spawn point is not in <paramref name="spawns"/>, or no longer grows back (BR-N5), is
    /// dropped, as it can never grow.
    /// </summary>
    public void RegrowNodes(IReadOnlyList<NodeSpawn> spawns, int nights)
    {
        ArgumentNullException.ThrowIfNull(spawns);
        ArgumentOutOfRangeException.ThrowIfLessThan(nights, 1);

        var spawnsById = new Dictionary<string, NodeSpawn>(StringComparer.Ordinal);
        foreach (var spawn in spawns)
        {
            ArgumentNullException.ThrowIfNull(spawn, nameof(spawns));
            spawnsById.TryAdd(spawn.Id, spawn);
        }

        _nodeRegrowths.RemoveAll(regrowth =>
        {
            // A spawn point gone from the data, or one that no longer grows back (BR-N5), never will.
            if (!spawnsById.TryGetValue(regrowth.SpawnId, out var spawn)
                || !spawn.Respawns
                || spawn.Kind is NodeKind.Stump or NodeKind.Debris)
            {
                return true;
            }

            var nightsPassed = Date.DaysSince(regrowth.ClearedOn) + 1;
            if (nightsPassed < nights || !IsFreeForRegrowth(spawn))
            {
                return false;
            }

            var node = ResourceNode.FromSpawn(spawn);
            _resourceNodes.Add(node);
            _nodesByTile.Add((node.Area, node.Position), node);
            return true;
        });
    }

    // BR-N4: the Chebyshev distance within which a tilled Tile blocks a farm spawn point.
    private const int RegrowthTilledClearance = 3;

    // BR-N4: no node already on the Tile and, on the farm, no tilled Tile in the 7 x 7 square clipped to the plot.
    private bool IsFreeForRegrowth(NodeSpawn spawn)
    {
        if (NodeAt(spawn.Area, spawn.Position) is not null)
        {
            return false;
        }

        if (spawn.Area != AreaId.Farm)
        {
            return true;
        }

        var (cx, cy) = (spawn.Position.X, spawn.Position.Y);
        for (var y = Math.Max(0, cy - RegrowthTilledClearance); y <= Math.Min(FarmPlot.Height - 1, cy + RegrowthTilledClearance); y++)
        {
            for (var x = Math.Max(0, cx - RegrowthTilledClearance); x <= Math.Min(FarmPlot.Width - 1, cx + RegrowthTilledClearance); x++)
            {
                if (FarmPlot[new GridPosition(x, y)].State == TileState.Tilled)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // BR-N1: which tool works on which node kind; glimmercaps are picked by hand.
    private static bool Works(ToolId tool, NodeKind kind) => tool switch
    {
        ToolId.Axe => kind is NodeKind.Tree or NodeKind.Stump,
        ToolId.Pickaxe => kind is NodeKind.Rock or NodeKind.RoskDeposit or NodeKind.GrothDeposit or NodeKind.Debris,
        _ => false,
    };
}
