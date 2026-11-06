using LongWayHome.Application.Data;
using LongWayHome.Application.Ports;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
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

    /// <summary>
    /// A session whose Nights never roll rain: its random source always returns 99, the highest roll (BR-G4). For tests
    /// and other callers that need dry, predictable Nights; the game uses the three-argument constructor.
    /// </summary>
    public GameSession(Game game, GameData data)
        : this(game, data, NeverRainSource.Instance)
    {
    }

    /// <summary>A session whose Nights roll the weather from the given random source (BR-G4).</summary>
    public GameSession(Game game, GameData data, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(random);

        Game = game;
        Data = data;
        _random = random;
    }

    /// <summary>Raised after a successful action changes a Tile.</summary>
    public event Action<GridPosition>? TileChanged;

    /// <summary>Raised after a successful action changes the Player (energy, can or tools).</summary>
    public event Action? PlayerChanged;

    /// <summary>Raised after a successful action changes the inventory.</summary>
    public event Action? InventoryChanged;

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

    /// <summary>The held-item cycle: Hoe, Watering can, then one entry per Seed stack in CropTypeId order.</summary>
    public IReadOnlyList<HeldItem> HeldItems()
    {
        var items = new List<HeldItem> { HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan) };

        // Inventory.Stacks is ordered by ItemKind then CropTypeId, so the seeds come out in CropTypeId order.
        items.AddRange(Game.Player.Inventory.Stacks
            .Where(stack => stack.Kind == ItemKind.Seed)
            .Select(stack => HeldItem.SeedOf(stack.CropTypeId)));

        return items.AsReadOnly();
    }

    /// <summary>Decides what Use would do, without changing state.</summary>
    public UsePreview PreviewUse(GridPosition? target, HeldItem held)
    {
        ArgumentNullException.ThrowIfNull(held);

        if (target is not { } position)
        {
            return Rejected(Rejection.NoTarget);
        }

        // BR-T6: harvesting needs no tool, so a Ripe Planting is harvested whatever is held.
        if (PlantingStatusAt(position) == PlantingStatus.Ripe)
        {
            return new UsePreview(UseActionKind.Harvest, null);
        }

        if (held.Seed is { } seed)
        {
            // BR-T3: the domain decides, so the preview and Game.Plant cannot drift apart.
            return PlantRejection(position, seed) is { } rejection
                ? Rejected(rejection)
                : new UsePreview(UseActionKind.Plant, null);
        }

        if (held.Tool == ToolId.Hoe)
        {
            // BR-T2, then BR-P2: the domain decides, so the preview and Game.Till cannot drift apart.
            if (Game.TillRejection(position, Data.Tuning.ToolEnergyCost) is { } rejection)
            {
                return Rejected(rejection);
            }

            return new UsePreview(UseActionKind.Till, null);
        }

        if (held.Tool == ToolId.WateringCan)
        {
            // BR-T9, BR-W2, then BR-P2: the domain decides, so the preview and Game.Water cannot drift apart.
            return Game.WaterRejection(position, Data.Tuning.ToolEnergyCost) is { } rejection
                ? Rejected(rejection)
                : new UsePreview(UseActionKind.Water, null);
        }

        return Rejected(Rejection.NothingToDo);
    }

    /// <summary>Performs what <see cref="PreviewUse"/> names, or returns its rejection.</summary>
    public ActionResult Use(GridPosition? target, HeldItem held)
    {
        var preview = PreviewUse(target, held);
        return preview.Action switch
        {
            UseActionKind.Till => Till(target!.Value),
            UseActionKind.Water => Water(target!.Value),
            UseActionKind.Plant => Plant(target!.Value, held.Seed!.Value),
            UseActionKind.Harvest => Harvest(target!.Value),
            _ => ActionResult.Rejected(preview.Rejection!.Value),
        };
    }

    /// <summary>Tills a Tile, spending the tuning's tool energy cost (BR-P2). Raises the change events on success.</summary>
    public ActionResult Till(GridPosition position)
    {
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
        var result = Game.Water(position, Data.Tuning.ToolEnergyCost);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            PlayerChanged?.Invoke();
        }

        return result;
    }

    /// <summary>Refills the can at the Pond (BR-W3). Raises PlayerChanged.</summary>
    public ActionResult RefillCan()
    {
        var result = Game.RefillCan();
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
    /// Harvests the ripe Planting on a Tile (BR-T6), looking up its Crop type in <see cref="GameData.CropTypes"/>.
    /// Raises InventoryChanged and TileChanged on success. A Planting whose Crop type has no data is NotRipe.
    /// </summary>
    public ActionResult Harvest(GridPosition position)
    {
        if (Game.FarmPlot[position].Planting is not { } planting)
        {
            return ActionResult.Rejected(Rejection.NoPlanting);
        }

        if (!Data.CropTypes.TryGetValue(planting.CropTypeId, out var cropType))
        {
            return ActionResult.Rejected(Rejection.NotRipe);
        }

        var result = Game.Harvest(position, cropType);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            InventoryChanged?.Invoke();
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
    /// Buys a Shop listing (BR-L2) through <see cref="ShopService.Buy"/>. Raises PlayerChanged and InventoryChanged on
    /// success and nothing on a rejection.
    /// </summary>
    public ActionResult Buy(string listingId, int quantity = 1)
    {
        var result = ShopService.Buy(Game, Data, listingId, quantity);
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
            ? $"{held.DisplayLabel} × {Game.Player.Inventory.Quantity(ItemKind.Seed, seed)}"
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

    // BR-T3, BR-C1: the domain decides, for both the preview and Plant. A Crop type with no data row has no Season.
    private Rejection? PlantRejection(GridPosition position, CropTypeId cropTypeId) =>
        Data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            ? Game.PlantRejection(position, cropType)
            : Game.PlantRejection(position, cropTypeId, season: null);

    /// <summary>
    /// Ends the Day (BR-G3): raises DayEnded, runs the Night, then raises NightCompleted. The part-minute carried by
    /// the clock is dropped so the next Day starts at exactly DayStartMinutes.
    /// </summary>
    private void EndDay(DayEndReason reason)
    {
        _clockRemainderMinutes = 0;
        DayEnded?.Invoke(reason);
        NightProcessor.Run(Game, Data, reason, _random);
        NightCompleted?.Invoke();
    }
}
