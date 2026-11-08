using LongWayHome.Application.Art;
using LongWayHome.Application.Data;
using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>Holds the current Game and the game data, and runs the Player's actions as commands.</summary>
public sealed class GameSession
{
    /// <summary>How close to a whole game minute the carried remainder must be to count as reaching it.</summary>
    private const double ClockEpsilonMinutes = 1e-9;

    /// <summary>The game minutes of play not yet added to the clock, always under one (BR-G2).</summary>
    private double _clockRemainderMinutes;

    /// <summary>The source of each Night's weather roll (BR-G4).</summary>
    private readonly IRandomSource _random;

    /// <summary>Where each Night saves the Game (BR-G5), in slot <see cref="_slot"/>.</summary>
    private readonly ISaveStore _store;

    private readonly int _slot;

    /// <summary>The clock the save step stamps <see cref="Game.SavedAt"/> from.</summary>
    private readonly TimeProvider _time;

    /// <summary>Whether this session has given Maggie's starter Seeds (BR-I1: at most once).</summary>
    private bool _starterSeedsGiven;

    /// <summary>
    /// A session whose Nights never roll rain: its random source always returns 99, the highest roll (BR-G4). For tests
    /// and other callers that need dry, predictable Nights; the game uses the six-argument constructor.
    /// </summary>
    public GameSession(Game game, GameData data)
        : this(game, data, NeverRainSource.Instance)
    {
    }

    /// <summary>A session whose Nights roll the weather from the given random source (BR-G4) and save nowhere.</summary>
    public GameSession(Game game, GameData data, IRandomSource random)
        : this(game, data, random, NoSaveStore.Instance, 1, TimeProvider.System)
    {
    }

    /// <summary>
    /// A session whose Nights roll the weather from the given random source (BR-G4) and end by marking the Game saved
    /// at <paramref name="time"/>'s current time and saving it to <paramref name="slot"/> of the store (BR-G5).
    /// </summary>
    public GameSession(Game game, GameData data, IRandomSource random, ISaveStore store, int slot, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(time);
        SaveSlots.ThrowIfOutOfRange(slot);

        Game = game;
        Data = data;
        _random = random;
        _store = store;
        _slot = slot;
        _time = time;
    }

    /// <summary>Raised with a one-line reason when the Night's save fails, before NightCompleted.</summary>
    public event Action<string>? SaveFailed;

    /// <summary>Raised after a successful action changes a Tile.</summary>
    public event Action<GridPosition>? TileChanged;

    /// <summary>Raised after a successful action changes the Player (energy, can or tools).</summary>
    public event Action? PlayerChanged;

    /// <summary>Raised after a successful action changes the inventory.</summary>
    public event Action? InventoryChanged;

    /// <summary>Raised after items drop on the ground or are picked up (BR-K3, BR-K4).</summary>
    public event Action? DroppedItemsChanged;

#pragma warning disable CS0067 // Red stub for #151: raised once the implementation lands.
    /// <summary>Raised when the Game's ResourceNodes change, and on NightCompleted.</summary>
    public event Action? NodesChanged;
#pragma warning restore CS0067

    /// <summary>The Game's ResourceNodes, for the node views.</summary>
    public IReadOnlyList<ResourceNode> ResourceNodes => [];

    /// <summary>Whether the last successful purchase dropped anything at the farmer's feet.</summary>
    public bool LastBuyDropped { get; private set; }

    /// <summary>Whether any DroppedItem lies at <paramref name="feet"/> in <see cref="CurrentArea"/>.</summary>
    public bool HasDroppedItemsAt(GridPosition feet) => Game.HasDroppedItemsAt(feet, CurrentArea);

    /// <summary>
    /// Picks up the DroppedItems at the farmer's feet in <see cref="CurrentArea"/> (BR-K4); items in other areas stay
    /// where they are. Raises InventoryChanged and DroppedItemsChanged when anything moved, and nothing otherwise.
    /// Costs no energy (BR-P2).
    /// </summary>
    public bool PickUp(GridPosition feet)
    {
        var picked = Game.PickUp(feet, CurrentArea);
        if (picked)
        {
            InventoryChanged?.Invoke();
            DroppedItemsChanged?.Invoke();
        }

        return picked;
    }

    /// <summary>
    /// The area the Player is in this session: the farm when the session is created (for a new or a loaded Game) and
    /// after every Night. Not saved.
    /// </summary>
    public AreaId CurrentArea { get; private set; } = AreaId.Farm;

    /// <summary>
    /// Whether the Player is where the FarmPlot's Tiles are: only on the farm. Off it a grid position names no Tile, so
    /// tools have no target there: Use, Till, Water, Plant and Harvest are Rejected(NoTarget) and change nothing.
    /// </summary>
    public bool IsOnFarmPlot => CurrentArea == AreaId.Farm;

    /// <summary>
    /// Raised after <see cref="EnterArea"/> moves the Player to another area. The Night's return to the farm does not
    /// raise it: NightCompleted already tells every listener that the new Day starts on the farm.
    /// </summary>
    public event Action<AreaId>? AreaChanged;

    /// <summary>
    /// Moves the Player to <paramref name="area"/>. An area that isn't open (BR-A1, <see cref="Game.IsAreaOpen"/>) is
    /// Rejected(AreaClosed) and changes nothing; entering the current area is Ok and raises nothing; otherwise
    /// CurrentArea changes and AreaChanged is raised. Costs no energy and does not move the clock.
    /// </summary>
    public ActionResult EnterArea(AreaId area)
    {
        if (!Game.IsAreaOpen(area))
        {
            return ActionResult.Rejected(Rejection.AreaClosed);
        }

        if (area != CurrentArea)
        {
            CurrentArea = area;
            AreaChanged?.Invoke(area);
        }

        return ActionResult.Ok;
    }

    /// <summary>Raised after <see cref="AdvanceClock"/> moves the clock by at least one minute.</summary>
    public event Action? ClockChanged;

    /// <summary>Raised when a Day ends, with the reason, before the Night runs.</summary>
    public event Action<DayEndReason>? DayEnded;

    /// <summary>Raised after the Night has run. The Night raises no TileChanged or ClockChanged; redraw on this.</summary>
    public event Action? NightCompleted;

    public Game Game { get; }

    public GameData Data { get; }

    /// <summary>
    /// Advances the clock by real seconds of play (BR-G2) at (DayEndMinutes − DayStartMinutes) / DayLengthRealSeconds
    /// game minutes per second, from <see cref="GameData.Tuning"/>. The part of a minute left over is kept, so short
    /// frames add up. Raises ClockChanged when the clock moves by at least one minute. When the clock reaches
    /// DayEndMinutes the Player passes out (BR-G3): the Day ends as PassedOut, the rest of the call's time is
    /// discarded and ClockChanged is not raised.
    /// </summary>
    public void AdvanceClock(double realSeconds)
    {
        // Written so NaN is refused too.
        if (!(realSeconds >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(realSeconds), realSeconds, "Real seconds must be 0 or more.");
        }

        var tuning = Data.Tuning;
        var minutesLeft = tuning.DayEndMinutes - Game.ClockMinutes;
        if (minutesLeft > 0)
        {
            // Multiply before dividing so a whole number of real seconds gives an exact number of game minutes.
            _clockRemainderMinutes +=
                realSeconds * (tuning.DayEndMinutes - tuning.DayStartMinutes) / tuning.DayLengthRealSeconds;

            // The epsilon absorbs the rounding error of many small frames (43 200 × 1/60 s must reach a whole day).
            var wholeMinutes = Math.Floor(_clockRemainderMinutes + ClockEpsilonMinutes);
            if (wholeMinutes < 1)
            {
                return;
            }

            if (wholeMinutes < minutesLeft)
            {
                _clockRemainderMinutes = Math.Max(0, _clockRemainderMinutes - wholeMinutes);
                Game.AdvanceClock((int)wholeMinutes, tuning.DayEndMinutes);
                ClockChanged?.Invoke();
                return;
            }

            Game.AdvanceClock(minutesLeft, tuning.DayEndMinutes);
        }

        // BR-G3: the clock has reached 02:00, so the Player passes out.
        EndDay(DayEndReason.PassedOut);
    }

    /// <summary>
    /// Ends the Day by sleeping at the house door (BR-G3). At or after DayEndMinutes it is TooLate and changes
    /// nothing; otherwise the Night runs and the Player wakes at the start of the next Day.
    /// </summary>
    public ActionResult Sleep()
    {
        // BR-G3: a Day ends exactly once; once the clock is at 02:00 it ends by passing out, not by sleep.
        if (Game.ClockMinutes >= Data.Tuning.DayEndMinutes)
        {
            return ActionResult.Rejected(Rejection.TooLate);
        }

        EndDay(DayEndReason.Slept);
        return ActionResult.Ok;
    }

    /// <summary>
    /// Moves the hotbar to the next Backpack row (BR-K5) and raises InventoryChanged. Costs no energy; nothing else
    /// changes.
    /// </summary>
    public void CycleHotbar()
    {
        Game.Player.Backpack.CycleHotbar();
        InventoryChanged?.Invoke();
    }

    /// <summary>
    /// Moves Backpack Slot <paramref name="from"/> onto <paramref name="to"/> through <see cref="Backpack.Move"/> and
    /// raises InventoryChanged when something moved. Costs no energy; nothing else changes.
    /// </summary>
    public bool MoveSlot(int from, int to)
    {
        if (!Game.Player.Backpack.Move(from, to))
        {
            return false;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Every Backpack Slot in index order with its label and icon, plus the hotbar row and the capacity.</summary>
    public BackpackView BackpackView()
    {
        var backpack = Game.Player.Backpack;
        var slots = backpack.Slots
            .Select(slot => new BackpackViewSlot(
                slot.Index,
                slot.Item,
                slot.Quantity,
                slot.ToolTier,
                BackpackText.Label(slot),
                slot.Item is { } item ? ItemIcons.For(item) : null))
            .ToList()
            .AsReadOnly();
        return new BackpackView(slots, backpack.HotbarRow, backpack.Capacity);
    }

    /// <summary>
    /// The six slots of the current hotbar row (BR-K5), position 0–5, with <paramref name="heldPosition"/> selected.
    /// Labels: "Hoe", "Watering can", "Emberroot seeds × 12" and "Emberroot × 7" with that Slot's quantity, and "" for
    /// an empty Slot. A position outside 0–5 throws.
    /// </summary>
    public IReadOnlyList<HotbarSlot> Hotbar(int heldPosition)
    {
        ThrowIfNotAPosition(heldPosition);

        return Game.Player.Backpack.HotbarSlots
            .Select((slot, position) => new HotbarSlot(
                HeldItemOf(slot),
                slot.Item,
                slot.Quantity,
                BackpackText.Label(slot),
                position == heldPosition))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// What the held position of the current row holds, or null when it is empty or holds a crop. A position outside
    /// 0–5 throws.
    /// </summary>
    public HeldItem? HeldItemAt(int heldPosition) => HeldItemOf(HotbarSlotAt(heldPosition));

    /// <summary>
    /// Decides what Use from the held position would do, without changing state: harvest a Ripe Planting whatever is
    /// held (BR-T6), else plant from a seed Slot (BR-T3), till with the hoe, water with the can; an empty or crop Slot
    /// is NothingToDo. A position outside 0–5 throws.
    /// </summary>
    public UsePreview PreviewUse(GridPosition? target, int heldPosition)
    {
        var slot = HotbarSlotAt(heldPosition);
        return PreviewUse(target, HeldItemOf(slot), slot.Index);
    }

    /// <summary>
    /// Performs what <see cref="PreviewUse(GridPosition?, int)"/> names from the held slot, or its rejection. Planting
    /// takes the Seed from that Slot; a harvested Crop that fits nowhere drops at <paramref name="feet"/>, or on the
    /// harvested Tile when it is null (BR-K3).
    /// </summary>
    public ActionResult Use(GridPosition? target, int heldPosition, GridPosition? feet = null)
    {
        var slot = HotbarSlotAt(heldPosition);
        var preview = PreviewUse(target, HeldItemOf(slot), slot.Index);
        return Perform(
            target,
            preview,
            position => PlantFromSlot(position, slot.Item!.Value.CropTypeId!.Value, slot.Index),
            feet);
    }

    /// <summary>
    /// Decides what Use would do with a held item, without changing state; a seed counts every Slot of it. The game
    /// uses <see cref="PreviewUse(GridPosition?, int)"/>; this stays for tests that compare against it.
    /// </summary>
    internal UsePreview PreviewUse(GridPosition? target, HeldItem held)
    {
        ArgumentNullException.ThrowIfNull(held);
        return PreviewUse(target, held, slotIndex: null);
    }

    /// <summary>
    /// Performs what <see cref="PreviewUse(GridPosition?, HeldItem)"/> names, or returns its rejection; a seed is taken
    /// from the highest-index Slot holding it.
    /// </summary>
    internal ActionResult Use(GridPosition? target, HeldItem held, GridPosition? feet = null)
    {
        var preview = PreviewUse(target, held);
        return Perform(target, preview, position => Plant(position, held.Seed!.Value), feet);
    }

    // The Use decision: harvest first (BR-T6), then the held item. With a slotIndex, a seed plants from that Slot only.
    private UsePreview PreviewUse(GridPosition? target, HeldItem? held, int? slotIndex)
    {
        // Tiles exist only on the farm: off the farm a grid position names no Tile, so there is no target.
        if (!IsOnFarmPlot || target is not { } position)
        {
            return Rejected(Rejection.NoTarget);
        }

        // BR-T6: harvesting needs no tool, so a Ripe Planting is harvested whatever is held.
        if (PlantingStatusAt(position) == PlantingStatus.Ripe)
        {
            // BR-T6: the domain decides, so the preview and Game.Harvest cannot drift apart; a full Backpack never
            // refuses (BR-K3). PlantingStatusAt is Ripe only for a Crop type with a data row.
            var cropType = Data.CropTypes[Game.FarmPlot[position].Planting!.CropTypeId];
            return Game.HarvestRejection(position, cropType) is { } harvestRejection
                ? Rejected(harvestRejection)
                : new UsePreview(UseActionKind.Harvest, null);
        }

        if (held?.Seed is { } seed)
        {
            // BR-T3: the domain decides, so the preview and Game.Plant cannot drift apart.
            var plantRejection = slotIndex is { } index
                ? PlantFromSlotRejection(position, seed, index)
                : PlantRejection(position, seed);
            return plantRejection is { } rejection
                ? Rejected(rejection)
                : new UsePreview(UseActionKind.Plant, null);
        }

        if (held?.Tool == ToolId.Hoe)
        {
            // BR-T2, then BR-P2: the domain decides, so the preview and Game.Till cannot drift apart.
            if (Game.TillRejection(position, Data.Tuning.ToolEnergyCost) is { } rejection)
            {
                return Rejected(rejection);
            }

            return new UsePreview(UseActionKind.Till, null);
        }

        if (held?.Tool == ToolId.WateringCan)
        {
            // BR-T9, BR-W2, then BR-P2: the domain decides, so the preview and Game.Water cannot drift apart.
            return Game.WaterRejection(position, Data.Tuning.ToolEnergyCost) is { } rejection
                ? Rejected(rejection)
                : new UsePreview(UseActionKind.Water, null);
        }

        return Rejected(Rejection.NothingToDo);
    }

    // Runs a previewed action; each Use overload says how it plants (from the held Slot, or the held seed type).
    private ActionResult Perform(
        GridPosition? target, UsePreview preview, Func<GridPosition, ActionResult> plant, GridPosition? feet) =>
        preview.Action switch
        {
            UseActionKind.Till => Till(target!.Value),
            UseActionKind.Water => Water(target!.Value),
            UseActionKind.Plant => plant(target!.Value),
            UseActionKind.Harvest => Harvest(target!.Value, feet),
            _ => ActionResult.Rejected(preview.Rejection!.Value),
        };

    /// <summary>Tills a Tile, spending the tuning's tool energy cost (BR-P2). Raises the change events on success.</summary>
    public ActionResult Till(GridPosition position)
    {
        if (!IsOnFarmPlot)
        {
            return ActionResult.Rejected(Rejection.NoTarget);
        }

        var result = Game.Till(position, Data.Tuning.ToolEnergyCost);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            PlayerChanged?.Invoke();
        }

        return result;
    }

    /// <summary>Waters a Tile (BR-T9, BR-W2, BR-P2). Raises TileChanged and PlayerChanged on success.</summary>
    public ActionResult Water(GridPosition position)
    {
        if (!IsOnFarmPlot)
        {
            return ActionResult.Rejected(Rejection.NoTarget);
        }

        var result = Game.Water(position, Data.Tuning.ToolEnergyCost);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            PlayerChanged?.Invoke();
        }

        return result;
    }

    /// <summary>Refills the can at <paramref name="source"/> (BR-W3). Raises PlayerChanged.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="WaterSource"/> member.</exception>
    public ActionResult RefillCan(WaterSource source)
    {
        var result = Game.RefillCan(source);
        if (result.IsOk)
        {
            PlayerChanged?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// Plants one Seed of a Crop type (BR-T3, BR-C1), looking up its Crop type in <see cref="GameData.CropTypes"/>.
    /// Raises InventoryChanged and TileChanged on success. A Seed whose Crop type has no data row is never planted:
    /// it is refused as OutOfSeason once the other checks pass.
    /// </summary>
    public ActionResult Plant(GridPosition position, CropTypeId cropTypeId)
    {
        if (!IsOnFarmPlot)
        {
            return ActionResult.Rejected(Rejection.NoTarget);
        }

        // A Crop type with no data row always has a rejection (OutOfSeason at the latest), so it is never planted.
        var result = Data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            ? Game.Plant(position, cropType)
            : ActionResult.Rejected(PlantRejection(position, cropTypeId)!.Value);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// Plants one Seed taken from one Backpack Slot (BR-T3, BR-C1), with the rejections of <see cref="Plant"/>.
    /// Raises InventoryChanged and TileChanged on success.
    /// </summary>
    private ActionResult PlantFromSlot(GridPosition position, CropTypeId cropTypeId, int slotIndex)
    {
        // A Crop type with no data row always has a rejection (OutOfSeason at the latest), so it is never planted.
        var result = Data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            ? Game.PlantFromSlot(position, cropType, slotIndex)
            : ActionResult.Rejected(PlantFromSlotRejection(position, cropTypeId, slotIndex)!.Value);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// Harvests the ripe Planting on a Tile (BR-T6), looking up its Crop type in <see cref="GameData.CropTypes"/>.
    /// Raises InventoryChanged and TileChanged on success, and DroppedItemsChanged when the Crop fit nowhere and dropped
    /// at <paramref name="feet"/> (the harvested Tile when null; BR-K3). A Planting whose Crop type has no data is
    /// NotRipe.
    /// </summary>
    public ActionResult Harvest(GridPosition position, GridPosition? feet = null)
    {
        if (!IsOnFarmPlot)
        {
            return ActionResult.Rejected(Rejection.NoTarget);
        }

        if (Game.FarmPlot[position].Planting is not { } planting)
        {
            return ActionResult.Rejected(Rejection.NoPlanting);
        }

        if (!Data.CropTypes.TryGetValue(planting.CropTypeId, out var cropType))
        {
            return ActionResult.Rejected(Rejection.NotRipe);
        }

        var droppedBefore = Game.DroppedItems.Count;
        var result = Game.Harvest(position, cropType, feet);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            InventoryChanged?.Invoke();
            RaiseIfDropped(droppedBefore);
        }

        return result;
    }

    /// <summary>Ships every Crop at the Shipping Bin (BR-S1). Raises InventoryChanged on success.</summary>
    public ActionResult ShipAll()
    {
        var result = Game.ShipAll();
        if (result.IsOk)
        {
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>The Shop listings offered today (BR-L1), from <see cref="ShopService.Offers"/>.</summary>
    public IReadOnlyList<ShopListing> ShopOffers() => ShopService.Offers(Game, Data);

    /// <summary>
    /// Buys a Shop listing (BR-L2) through <see cref="ShopService.Buy"/>; seeds that fit nowhere drop at
    /// <paramref name="feet"/> (BR-K3). Raises PlayerChanged and InventoryChanged on success, DroppedItemsChanged when
    /// any dropped, and nothing on a rejection. A success sets <see cref="LastBuyDropped"/>.
    /// </summary>
    public ActionResult Buy(string listingId, int quantity = 1, GridPosition? feet = null)
    {
        var droppedBefore = Game.DroppedItems.Count;
        var result = ShopService.Buy(Game, Data, listingId, quantity, feet);
        if (result.IsOk)
        {
            // BR-K3: a gain only ever adds DroppedItems, so a changed count means something dropped.
            LastBuyDropped = Game.DroppedItems.Count != droppedBefore;
            PlayerChanged?.Invoke();
            InventoryChanged?.Invoke();
            if (LastBuyDropped)
            {
                DroppedItemsChanged?.Invoke();
            }
        }

        return result;
    }

    /// <summary>
    /// Whether Maggie buys Crops here: only while the Player is in town, at the general store (BR-L3). The shop menu
    /// shows its Sell section only when this is true.
    /// </summary>
    public bool CanSellToMaggie => CurrentArea == AreaId.Town;

    /// <summary>
    /// The Sell section's rows (BR-L3): each carried Crop type with a data row, ordered by CropTypeId, with its name from
    /// data, the quantity carried and what <see cref="SellToMaggie"/> would pay. Types with no data row are left out.
    /// </summary>
    public IReadOnlyList<SellableCrop> SellableCrops() =>
        Game.Player.Backpack.Totals(ItemKind.Crop)
            .Where(stack => Data.CropTypes.ContainsKey(stack.CropTypeId))
            .Select(stack => new SellableCrop(
                stack.CropTypeId,
                Data.CropTypes[stack.CropTypeId].Name,
                stack.Quantity,
                Game.SellPayout(stack.CropTypeId, Data.CropTypes)))
            .ToList();

    /// <summary>
    /// What <see cref="SellAllToMaggie"/> would pay (BR-L3), from <see cref="Game.SellAllPayout"/>, changing nothing.
    /// Throws on overflow (BR-P5).
    /// </summary>
    public int SellAllPayout() => Game.SellAllPayout(Data.CropTypes);

    /// <summary>
    /// Sells every carried Crop of one type to Maggie through <see cref="Game.SellCrop"/>, paid at once (BR-L3).
    /// NotOffered and no change outside town. Raises PlayerChanged and InventoryChanged on success, and nothing on a
    /// rejection.
    /// </summary>
    public ActionResult SellToMaggie(CropTypeId cropTypeId) =>
        RaiseIfSold(CanSellToMaggie ? Game.SellCrop(cropTypeId, Data.CropTypes) : ActionResult.Rejected(Rejection.NotOffered));

    /// <summary>
    /// Sells every carried Crop with a data row to Maggie through <see cref="Game.SellAllCrops"/>, as one step (BR-L3).
    /// NotOffered and no change outside town. Raises PlayerChanged and InventoryChanged on success, and nothing on a
    /// rejection.
    /// </summary>
    public ActionResult SellAllToMaggie() =>
        RaiseIfSold(CanSellToMaggie ? Game.SellAllCrops(Data.CropTypes) : ActionResult.Rejected(Rejection.NotOffered));

    private ActionResult RaiseIfSold(ActionResult result)
    {
        if (result.IsOk)
        {
            PlayerChanged?.Invoke();
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>The HUD label for a held item: "Hoe", "Watering can", or "Emberroot seeds × 9".</summary>
    public string HeldLabel(HeldItem held)
    {
        ArgumentNullException.ThrowIfNull(held);

        return held.Seed is { } seed
            ? $"{held.DisplayLabel} × {Game.Player.Backpack.Quantity(ItemRef.Seed(seed))}"
            : held.DisplayLabel;
    }

    /// <summary>
    /// The status of the Planting on a Tile, or null when it has none. A Planting whose Crop type has no data row is
    /// Growing, never Ripe.
    /// </summary>
    public PlantingStatus? PlantingStatusAt(GridPosition position) =>
        Game.FarmPlot[position].Planting is not { } planting
            ? null
            : Data.CropTypes.TryGetValue(planting.CropTypeId, out var cropType)
                ? planting.StatusFor(cropType)
                : PlantingStatus.Growing;

    /// <summary>
    /// The <see cref="GrowthStage"/> the Planting on a Tile is drawn as, or null when it has none. A Planting whose
    /// Crop type has no data row is Seed at 0 days and Sprout after, never Ripe (as in <see cref="PlantingStatusAt"/>).
    /// </summary>
    public GrowthStage? GrowthStageAt(GridPosition position) =>
        Game.FarmPlot[position].Planting is not { } planting
            ? null
            : Data.CropTypes.TryGetValue(planting.CropTypeId, out var cropType)
                ? GrowthStages.For(planting, cropType)
                : planting.DaysGrown == 0 ? GrowthStage.Seed : GrowthStage.Sprout;

    private static UsePreview Rejected(Rejection rejection) => new(null, rejection);

    // BR-K3: a gain only ever adds DroppedItems, so a changed count means something dropped.
    private void RaiseIfDropped(int droppedBefore)
    {
        if (Game.DroppedItems.Count != droppedBefore)
        {
            DroppedItemsChanged?.Invoke();
        }
    }

    // BR-T3, BR-C1: the domain decides, for both the preview and Plant. A Crop type with no data row has no Season.
    private Rejection? PlantRejection(GridPosition position, CropTypeId cropTypeId) =>
        Data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            ? Game.PlantRejection(position, cropType)
            : Game.PlantRejection(position, cropTypeId, season: null);

    // BR-T3, BR-C1: as PlantRejection, with the Seed taken from one Slot. A Crop type with no data row has no Season.
    private Rejection? PlantFromSlotRejection(GridPosition position, CropTypeId cropTypeId, int slotIndex) =>
        Data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            ? Game.PlantFromSlotRejection(position, cropType, slotIndex)
            : Game.PlantFromSlotRejection(position, cropTypeId, season: null, slotIndex);

    private static void ThrowIfNotAPosition(int heldPosition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(heldPosition);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(heldPosition, Backpack.RowLength);
    }

    // A tool or a seed is held; a crop (or any other item) and an empty Slot hold nothing.
    private static HeldItem? HeldItemOf(BackpackSlot slot) => slot.Item switch
    {
        { Kind: ItemKind.Tool, ToolId: { } tool } => HeldItem.Of(tool),
        { Kind: ItemKind.Seed, CropTypeId: { } seed } => HeldItem.SeedOf(seed),
        _ => null,
    };

    // Called every frame by the Use prompt, so it reads the one Slot rather than copying the row.
    private BackpackSlot HotbarSlotAt(int heldPosition) => Game.Player.Backpack.HotbarSlotAt(heldPosition);

    /// <summary>
    /// Maggie hands over the tuning's starting Seeds (BR-P6): <see cref="Game.GiveStarterSeeds"/>. Refused OpeningOver
    /// once the Opening is done or when this session has already given them (BR-I1), so the sequence can't give them
    /// twice. Raises InventoryChanged on success.
    /// </summary>
    public ActionResult GiveStarterSeeds()
    {
        // BR-I1: once per session, whatever the Game says.
        if (_starterSeedsGiven)
        {
            return ActionResult.Rejected(Rejection.OpeningOver);
        }

        // BR-P6: the seeds come from Maggie, and they are the tuning's starting Seeds.
        var result = Game.GiveStarterSeeds(Data.Tuning.StartingSeeds);
        if (result.IsOk)
        {
            _starterSeedsGiven = true;
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>Marks the Opening done (BR-I1).</summary>
    public void CompleteOpening() => Game.CompleteOpening();

    /// <summary>
    /// Ends the Day (BR-G3): raises DayEnded, runs the Night ending with the save (BR-G5), then raises NightCompleted.
    /// A failed save does not undo the Night: it raises SaveFailed with the reason, before NightCompleted. The
    /// part-minute carried by the clock is dropped so the next Day starts at exactly DayStartMinutes, and the Player
    /// wakes on the farm whichever area the Day ended in: CurrentArea is Farm when NightCompleted is raised (without
    /// AreaChanged).
    /// </summary>
    private void EndDay(DayEndReason reason)
    {
        _clockRemainderMinutes = 0;

        // BR-I1: a saved Game never holds a half-finished Opening. A Day that ends before Maggie has finished gives
        // the starter Seeds (BR-P6) if this session hasn't, and completes the Opening, before the Night saves (BR-G5).
        if (!Game.OpeningDone)
        {
            if (!_starterSeedsGiven)
            {
                GiveStarterSeeds();
            }

            CompleteOpening();
        }

        DayEnded?.Invoke(reason);

        string? saveFailure = null;
        NightProcessor.Run(Game, Data, reason, _random, game => saveFailure = Save(game));

        if (saveFailure is not null)
        {
            SaveFailed?.Invoke(saveFailure);
        }

        // Every Day starts at the farmhouse door.
        CurrentArea = AreaId.Farm;
        NightCompleted?.Invoke();
    }

    /// <summary>
    /// The Night's save step (BR-G5): marks the Game saved now and writes it to the session's slot. Returns the reason
    /// when the store fails, or null when it saved (or there is no store). A failed save puts SavedAt back.
    /// </summary>
    private string? Save(Game game)
    {
        // The sessions built without a save store save nowhere, so they leave SavedAt as it was.
        if (ReferenceEquals(_store, NoSaveStore.Instance))
        {
            return null;
        }

        var previousSavedAt = game.SavedAt;
        game.MarkSaved(_time.GetUtcNow());
        try
        {
            _store.Save(_slot, game);
            return null;
        }
        catch (SaveStoreException exception)
        {
            game.MarkSaved(previousSavedAt);
            return exception.Reason;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            game.MarkSaved(previousSavedAt);
            return exception.Message;
        }
    }
}
