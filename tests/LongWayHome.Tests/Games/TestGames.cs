using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;
using LongWayHome.Infrastructure.Data;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Builds Games and sessions for action tests from the real <c>game/data/*.json</c> files.</summary>
internal static class TestGames
{
    public static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 30, 0, TimeSpan.Zero);

    public static readonly GameData Shipped = LoadShippedData();

    /// <summary>The shipped data without one Crop type's row, for the paths a Crop type with no data row takes.</summary>
    public static GameData ShippedWithout(CropTypeId cropTypeId) => Shipped with
    {
        CropTypes = Shipped.CropTypes
            .Where(pair => pair.Key != cropTypeId)
            .ToDictionary(pair => pair.Key, pair => pair.Value),
    };

    /// <summary>A Game on a fresh plot whose Player has the given energy and Backpack (the two tools only by default).</summary>
    public static GameState WithEnergy(int energy, Backpack? backpack = null) =>
        new(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            new Player(new Gold(100), energy, new WateringCan(20), backpack ?? ToolsOnly()),
            new FarmPlot());

    /// <summary>
    /// A Game on a fresh plot at 150 energy whose Player has the given gold, on the given date (Spring 1 by default),
    /// holding the given Backpack (the tools and starting seeds by default) and the given Watering can (a full 20-charge can by
    /// default).
    /// </summary>
    public static GameState WithGold(int gold, GameDate? date = null, Backpack? backpack = null, WateringCan? can = null) =>
        new(
            Guid.NewGuid(),
            date ?? FirstDay,
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            new Player(new Gold(gold), Player.MaxEnergy, can ?? new WateringCan(20), backpack ?? StartingSeeds()),
            new FarmPlot());

    /// <summary>
    /// A Game on a fresh plot (Year 1, Spring, Day 1) with 100 gold, the given energy and Backpack, and a Shipping
    /// Bin holding the given items, passed through the Game constructor.
    /// </summary>
    public static GameState WithShippingBin(int energy, Backpack? backpack, params (CropTypeId CropTypeId, int Quantity)[] items) =>
        OnDateWithShippingBin(FirstDay, energy, backpack, [], items);

    /// <summary>As <see cref="WithShippingBin"/>, on the given date, grass except for the given Tiles.</summary>
    public static GameState OnDateWithShippingBin(
        GameDate date, int energy, Backpack? backpack, Tile[] tiles, params (CropTypeId CropTypeId, int Quantity)[] items)
    {
        var plot = OnDate(date, energy, new WateringCan(20), backpack ?? ToolsOnly(), tiles);
        var bin = new ShippingBin();
        foreach (var (cropTypeId, quantity) in items)
        {
            bin.Add(cropTypeId, quantity);
        }

        return new GameState(
            plot.Id,
            plot.Date,
            plot.ClockMinutes,
            plot.Weather,
            plot.LastDayEnd,
            plot.CreatedAt,
            plot.SavedAt,
            plot.Player,
            plot.FarmPlot,
            bin);
    }

    /// <summary>
    /// A session over a new Game once Maggie has handed over the starter Seeds (BR-P6, #139): the hoe, the can, 10
    /// emberroot and 5 dusktuber. The Opening is still not done.
    /// </summary>
    /// <summary>
    /// The shipped data with no node spawn points (#151), for tests that need every Tile of a new Game's plot to be
    /// tillable rather than overgrown.
    /// </summary>
    public static readonly GameData ShippedWithoutNodes = Shipped with { NodeSpawns = [] };

    public static GameSession NewSession(GameData? data = null)
    {
        data ??= Shipped;
        var session = new GameSession(NewGameFactory.Create(data, Now), data);
        session.GiveStarterSeeds();
        return session;
    }

    /// <summary>A new Game holding the starter Seeds Maggie gives in the Opening (BR-P6, #139).</summary>
    public static GameState NewGameWithSeeds() => NewSession().Game;

    public static GameSession SessionWithEnergy(int energy, GameData? data = null) =>
        new(WithEnergy(energy), data ?? Shipped);

    /// <summary>Energy plus every Tile's state and watered flag, to prove an action changed nothing.</summary>
    public static (int Energy, string Tiles) Snapshot(GameState game) =>
        (game.Player.Energy,
            string.Join(",", game.FarmPlot.Tiles.Select(tile => $"{tile.Position.X}:{tile.Position.Y}:{tile.State}:{tile.Watered}")));

    /// <summary>The shipped data row for a Crop type (emberroot growDays 3, dusktuber 5, moonmelon 8).</summary>
    public static CropType Crop(CropTypeId id) => Shipped.CropTypes[id];

    /// <summary>The date a new Game starts on: Year 1, Spring, Day 1.</summary>
    public static GameDate FirstDay => new(1, Season.Spring, 1);

    /// <summary>A Planting of a Crop type, planted on the first day, that has grown for the given days.</summary>
    public static Planting PlantingOf(CropTypeId id, int daysGrown) => new(id, FirstDay, daysGrown);

    /// <summary>A Planting that is Ripe for its shipped Crop type.</summary>
    public static Planting Ripe(CropTypeId id) => PlantingOf(id, Crop(id).GrowDays);

    /// <summary>A tilled Tile holding a Planting.</summary>
    public static Tile TilledWith(GridPosition position, Planting? planting, bool watered = false) =>
        new(position, TileState.Tilled, watered, planting);

    /// <summary>The two tools and the shipped starting seeds: 10 emberroot (Slot 2) and 5 dusktuber (Slot 3).</summary>
    public static Backpack StartingSeeds() => StartingBackpack();

    /// <summary>A Game whose plot is grass except for the given Tiles (restored through FarmPlot(IEnumerable&lt;Tile&gt;)).</summary>
    public static GameState WithTiles(int energy, Backpack? backpack, params Tile[] tiles) =>
        WithCan(energy, new WateringCan(20), backpack, tiles);

    /// <summary>A Game with the given energy and Watering can, grass except for the given Tiles.</summary>
    public static GameState WithCan(int energy, WateringCan can, Backpack? backpack, params Tile[] tiles) =>
        OnDate(FirstDay, energy, can, backpack, tiles);

    /// <summary>A Game on the given date at 150 energy, grass except for the given Tiles.</summary>
    public static GameState OnDate(GameDate date, Backpack? backpack, params Tile[] tiles) =>
        OnDate(date, 150, new WateringCan(20), backpack, tiles);

    /// <summary>
    /// The canonical Crop type rows (canonical-values.md), built here rather than read from the shipped data so domain
    /// tests do not depend on which rows the data files ship.
    /// </summary>
    public static readonly IReadOnlyDictionary<CropTypeId, CropType> CanonicalCrops = new Dictionary<CropTypeId, CropType>
    {
        [CropTypeId.Emberroot] = new(CropTypeId.Emberroot, "Emberroot", 3, new Gold(15), new Gold(35), Season.Spring, false),
        [CropTypeId.Dusktuber] = new(CropTypeId.Dusktuber, "Dusktuber", 5, new Gold(30), new Gold(75), Season.Spring, false),
        [CropTypeId.Moonmelon] = new(CropTypeId.Moonmelon, "Moonmelon", 8, new Gold(60), new Gold(190), Season.Fall, false),
        [CropTypeId.Sunbell] = new(CropTypeId.Sunbell, "Sunbell", 4, new Gold(20), new Gold(50), Season.Summer, false),
        [CropTypeId.Starcorn] = new(CropTypeId.Starcorn, "Starcorn", 7, new Gold(45), new Gold(130), Season.Summer, false),
    };

    /// <summary>A Game on the given date with the given energy and Watering can, grass except for the given Tiles.</summary>
    public static GameState OnDate(GameDate date, int energy, WateringCan can, Backpack? backpack, params Tile[] tiles)
    {
        var overrides = tiles.ToDictionary(tile => tile.Position);
        var all = new List<Tile>();
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                all.Add(overrides.TryGetValue(position, out var tile) ? tile : new Tile(position));
            }
        }

        return new GameState(
            Guid.NewGuid(),
            date,
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            new Player(new Gold(100), energy, can, backpack ?? StartingSeeds()),
            new FarmPlot(all));
    }

    /// <summary>A can of the shipped capacity holding the given charges.</summary>
    public static WateringCan CanWith(int charges) => new(Shipped.Tuning.CanCapacity, charges, false);

    /// <summary>A session with the given energy, can and Tiles, and the starting seeds.</summary>
    public static GameSession SessionWithCan(int energy, WateringCan can, params Tile[] tiles) =>
        new(WithCan(energy, can, null, tiles), Shipped);

    /// <summary>The full snapshot plus the can's charges, to prove a watering action changed nothing.</summary>
    public static ((int Energy, string Tiles, string Inventory) Full, int Charges) WaterSnapshot(GameState game) =>
        (FullSnapshot(game), game.Player.WateringCan.Charges);

    /// <summary>A session at 150 energy with the starting seeds and the given Tiles.</summary>
    public static GameSession SessionWithTiles(params Tile[] tiles) => new(WithTiles(150, null, tiles), Shipped);

    /// <summary>Energy, every Tile (state, watered, planting) and every Backpack Slot, to prove an action changed nothing.</summary>
    public static (int Energy, string Tiles, string Inventory) FullSnapshot(GameState game) =>
        (game.Player.Energy,
            string.Join(
                ",",
                game.FarmPlot.Tiles.Select(tile =>
                    $"{tile.Position.X}:{tile.Position.Y}:{tile.State}:{tile.Watered}:{tile.Planting}")),
            Describe(game.Player.Backpack));

    /// <summary>The Crop that fills the free Slots of <see cref="FullBackpack"/>; tests using it avoid this Crop.</summary>
    public static readonly ItemRef Filler = ItemRef.Crop(CropTypeId.Starcorn);

    /// <summary>A 12-Slot Backpack holding the Basic hoe in Slot 0 and the Basic Watering can in Slot 1.</summary>
    public static Backpack ToolsOnly()
    {
        var backpack = new Backpack();
        backpack.AddTool(ToolId.Hoe, ToolTier.Basic);
        backpack.AddTool(ToolId.WateringCan, ToolTier.Basic);
        return backpack;
    }

    /// <summary>The two tools, then the given items added in order through <see cref="Backpack.Add"/>.</summary>
    public static Backpack BackpackWith(params (ItemRef Item, int Quantity)[] items)
    {
        var backpack = ToolsOnly();
        foreach (var (item, quantity) in items)
        {
            if (backpack.Add(item, quantity) != 0)
            {
                throw new InvalidOperationException($"{quantity} of {item} do not fit in the test Backpack.");
            }
        }

        return backpack;
    }

    /// <summary>
    /// A 12-Slot Backpack restored through its constructor on hotbar row 0: the Basic hoe in Slot 0, the Basic Watering
    /// can in Slot 1, the given Slots, the rest empty. Lets a test put the same item in chosen Slots.
    /// </summary>
    public static Backpack RestoredBackpack(params BackpackSlot[] slots)
    {
        var given = slots.ToDictionary(slot => slot.Index);
        var all = Enumerable.Range(0, Backpack.StartingCapacity).Select(index => index switch
        {
            0 => new BackpackSlot(0, ItemRef.Tool(ToolId.Hoe), 1, ToolTier.Basic),
            1 => new BackpackSlot(1, ItemRef.Tool(ToolId.WateringCan), 1, ToolTier.Basic),
            _ => given.TryGetValue(index, out var slot) ? slot : new BackpackSlot(index, null, 0, null),
        });
        return new Backpack(Backpack.StartingCapacity, 0, all);
    }

    /// <summary>
    /// A 12-Slot Backpack whose Slots are all told apart: the tools in 0 and 1, then the five seeds (1 to 5 each, in
    /// CropTypeId order) in 2 to 6 and the five crops (6 to 10 each) in 7 to 11.
    /// </summary>
    public static Backpack DistinctBackpack() => BackpackWith(
        (ItemRef.Seed(CropTypeId.Emberroot), 1),
        (ItemRef.Seed(CropTypeId.Dusktuber), 2),
        (ItemRef.Seed(CropTypeId.Moonmelon), 3),
        (ItemRef.Seed(CropTypeId.Sunbell), 4),
        (ItemRef.Seed(CropTypeId.Starcorn), 5),
        (ItemRef.Crop(CropTypeId.Emberroot), 6),
        (ItemRef.Crop(CropTypeId.Dusktuber), 7),
        (ItemRef.Crop(CropTypeId.Moonmelon), 8),
        (ItemRef.Crop(CropTypeId.Sunbell), 9),
        (ItemRef.Crop(CropTypeId.Starcorn), 10));

    /// <summary>
    /// The tools and seeds the current hotbar row holds, in slot order; empty and crop slots hold none (#129: replaces
    /// the removed <c>GameSession.HeldItems()</c>).
    /// </summary>
    public static IReadOnlyList<HeldItem> HeldItemsInHotbar(GameSession session) =>
        session.Hotbar(0).Select(slot => slot.Item).OfType<HeldItem>().ToList();

    /// <summary>The two tools then the shipped starting seeds: 10 emberroot (Slot 2) and 5 dusktuber (Slot 3).</summary>
    public static Backpack StartingBackpack() =>
        BackpackWith((ItemRef.Seed(CropTypeId.Emberroot), 10), (ItemRef.Seed(CropTypeId.Dusktuber), 5));

    /// <summary>As <see cref="BackpackWith"/>, then every free Slot filled with 99 of <see cref="Filler"/>.</summary>
    public static Backpack FullBackpack(params (ItemRef Item, int Quantity)[] items)
    {
        var backpack = BackpackWith(items);
        var free = backpack.Slots.Count(slot => slot.Item is null);
        if (free > 0 && backpack.Add(Filler, free * Backpack.MaxStack) != 0)
        {
            throw new InvalidOperationException("The filler did not fit.");
        }

        return backpack;
    }

    /// <summary>
    /// A Game whose Player carries the given Backpack, with the given gold (100), energy (150), Watering can (a full
    /// 20-charge can), date (Spring 1) and Shipping Bin, grass except for the given Tiles.
    /// </summary>
    public static GameState WithBackpack(
        Backpack backpack,
        int gold = 100,
        int energy = 150,
        WateringCan? can = null,
        GameDate? date = null,
        ShippingBin? bin = null,
        params Tile[] tiles)
    {
        var overrides = tiles.ToDictionary(tile => tile.Position);
        var all = new List<Tile>();
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                all.Add(overrides.TryGetValue(position, out var tile) ? tile : new Tile(position));
            }
        }

        return new GameState(
            Guid.NewGuid(),
            date ?? FirstDay,
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            new Player(new Gold(gold), energy, can ?? new WateringCan(20), backpack),
            new FarmPlot(all),
            bin);
    }

    /// <summary>A session at 150 energy whose Player carries the given Backpack, grass except for the given Tiles.</summary>
    public static GameSession SessionWithBackpack(Backpack backpack, params Tile[] tiles) =>
        new(WithBackpack(backpack, tiles: tiles), Shipped);

    /// <summary>
    /// The non-tool items carried, one per (kind, Crop type) with its total across Slots, ordered by kind then Crop
    /// type: what the Phase 1 inventory's stacks were.
    /// </summary>
    public static IReadOnlyList<(ItemKind Kind, CropTypeId CropTypeId, int Quantity)> Stacks(Backpack backpack) =>
        new[] { ItemKind.Seed, ItemKind.Crop }
            .SelectMany(kind => backpack.Totals(kind).Select(total => (kind, total.CropTypeId, total.Quantity)))
            .ToList();

    /// <summary>Every Slot as "index:kind:id:quantity:tier", to compare Backpacks or prove nothing changed.</summary>
    public static string Describe(Backpack backpack) =>
        string.Join(",", backpack.Slots.Select(DescribeSlot));

    /// <summary>One Slot as "index:kind:id:quantity:tier"; an empty Slot is "index:-:-:0:-".</summary>
    public static string DescribeSlot(BackpackSlot slot) =>
        $"{slot.Index}:{slot.Item?.Kind.ToString() ?? "-"}:{(object?)slot.Item?.CropTypeId ?? (object?)slot.Item?.ToolId ?? "-"}:{slot.Quantity}:{slot.ToolTier?.ToString() ?? "-"}";

    private static GameData LoadShippedData()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "data");
        string Read(string fileName) => File.ReadAllText(Path.Combine(folder, fileName));

        return new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName),
            Read(JsonGameDataSource.NodesFileName)).Load();
    }
}
