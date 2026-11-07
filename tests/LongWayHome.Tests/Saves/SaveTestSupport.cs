using System.Text.Json.Nodes;
using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>A fresh, empty folder under the temp path for one test, deleted on Dispose.</summary>
internal sealed class TempSaveFolder : IDisposable
{
    public TempSaveFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lwh-saves-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary><c>&lt;folder&gt;/slot-&lt;n&gt;.json</c> plus an optional suffix such as <c>.bak</c>.</summary>
    public string SlotFile(int slot, string suffix = "") => System.IO.Path.Combine(Path, $"slot-{slot}.json{suffix}");

    public JsonObject ReadSlot(int slot, string suffix = "") =>
        JsonNode.Parse(File.ReadAllText(SlotFile(slot, suffix)))!.AsObject();

    public void WriteSlot(int slot, JsonNode document, string suffix = "") =>
        File.WriteAllText(SlotFile(slot, suffix), document.ToJsonString());

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

/// <summary>A TimeProvider whose UTC time is fixed.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

/// <summary>A random source that always returns the same roll (0 is rain, 99 never is).</summary>
internal sealed class FixedRandom(int roll) : IRandomSource
{
    public int NextPercent() => roll;
}

/// <summary>A save store whose Save throws the given exception; List and Load are never expected.</summary>
internal sealed class ThrowingSaveStore(Exception toThrow) : ISaveStore
{
    public int Saves { get; private set; }

    public IReadOnlyList<SaveSlotInfo> List() => throw new InvalidOperationException("Not used by these tests.");

    public GameState Load(int slot) => throw new InvalidOperationException("Not used by these tests.");

    public void Save(int slot, GameState game)
    {
        Saves++;
        throw toThrow;
    }
}

/// <summary>A save store that records each Save call's slot; List and Load are never expected.</summary>
internal sealed class CountingSaveStore : ISaveStore
{
    public List<int> SavedSlots { get; } = [];

    public IReadOnlyList<SaveSlotInfo> List() => throw new InvalidOperationException("Not used by these tests.");

    public GameState Load(int slot) => throw new InvalidOperationException("Not used by these tests.");

    public void Save(int slot, GameState game) => SavedSlots.Add(slot);
}

/// <summary>Games for save tests: every saved attribute set to something other than its default.</summary>
internal static class SaveGames
{
    public static readonly DateTimeOffset CreatedAt = new(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(1));
    public static readonly DateTimeOffset SavedAt = new(2026, 11, 2, 21, 15, 30, TimeSpan.Zero);

    public static readonly GridPosition Growing = new(3, 2);
    public static readonly GridPosition WateredEmpty = new(4, 2);
    public static readonly GridPosition DryEmpty = new(11, 8);

    /// <summary>
    /// A Game in Summer, Year 2, Day 14 at 13:00 on a rain Day after a Pass out, with 1 234 gold, 77 energy, both
    /// tools, an upgraded can at 13 of 40, seeds and crops, three tilled Tiles (one with a Planting) and a Shipping Bin
    /// holding two Crop types.
    /// </summary>
    public static GameState Rich(
        Guid? id = null,
        int gold = 1234,
        GameDate? date = null,
        DayEndReason? lastDayEnd = DayEndReason.PassedOut,
        int clock = 780,
        Weather weather = Weather.Rain,
        bool withBin = true)
    {
        // The Can upgrade was bought, so the can is a Rosk can (BR-W1).
        var inventory = TestGames.ToolsOnly();
        inventory.SetTier(ToolId.WateringCan, ToolTier.Rosk);
        inventory.Add(ItemRef.Seed(CropTypeId.Sunbell), 4);
        inventory.Add(ItemRef.Seed(CropTypeId.Starcorn), 2);
        inventory.Add(ItemRef.Crop(CropTypeId.Emberroot), 7);

        var tiles = new List<Tile>();
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                tiles.Add(position switch
                {
                    _ when position == Growing => new Tile(
                        position, TileState.Tilled, true, new Planting(CropTypeId.Starcorn, new GameDate(2, Season.Summer, 9), 4)),
                    _ when position == WateredEmpty => new Tile(position, TileState.Tilled, true, null),
                    _ when position == DryEmpty => new Tile(position, TileState.Tilled, false, null),
                    _ => new Tile(position),
                });
            }
        }

        var bin = new ShippingBin();
        if (withBin)
        {
            bin.Add(CropTypeId.Sunbell, 3);
            bin.Add(CropTypeId.Emberroot, 12);
        }

        return new GameState(
            id ?? Guid.NewGuid(),
            date ?? new GameDate(2, Season.Summer, 14),
            clock,
            weather,
            lastDayEnd,
            CreatedAt,
            SavedAt,
            new Player(new Gold(gold), 77, new WateringCan(40, 13, true), inventory),
            new FarmPlot(tiles),
            bin);
    }

    /// <summary>A plain new-game-like Game on Spring 1 at 06:00 with the given gold.</summary>
    public static GameState Plain(int gold = 100) => TestGames.WithGold(gold);
}

/// <summary>Compares every attribute a save holds.</summary>
internal static class SavedState
{
    public static void AssertSame(GameState expected, GameState actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Date, actual.Date);
        Assert.Equal(expected.ClockMinutes, actual.ClockMinutes);
        Assert.Equal(expected.Weather, actual.Weather);
        Assert.Equal(expected.LastDayEnd, actual.LastDayEnd);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.SavedAt, actual.SavedAt);

        Assert.Equal(expected.Player.Gold, actual.Player.Gold);
        Assert.Equal(expected.Player.Energy, actual.Player.Energy);
        Assert.Equal(expected.Player.WateringCan.Capacity, actual.Player.WateringCan.Capacity);
        Assert.Equal(expected.Player.WateringCan.Charges, actual.Player.WateringCan.Charges);
        Assert.Equal(expected.Player.WateringCan.Upgraded, actual.Player.WateringCan.Upgraded);
        Assert.Equal(expected.Player.Backpack.Capacity, actual.Player.Backpack.Capacity);
        Assert.Equal(expected.Player.Backpack.HotbarRow, actual.Player.Backpack.HotbarRow);
        Assert.Equal(TestGames.Describe(expected.Player.Backpack), TestGames.Describe(actual.Player.Backpack));

        Assert.Equal(expected.FarmPlot.Tiles.Count, actual.FarmPlot.Tiles.Count);
        foreach (var tile in expected.FarmPlot.Tiles)
        {
            var loaded = actual.FarmPlot[tile.Position];
            Assert.Equal(tile.Position, loaded.Position);
            Assert.Equal(tile.State, loaded.State);
            Assert.Equal(tile.Watered, loaded.Watered);
            Assert.Equal(tile.Planting, loaded.Planting);
        }

        Assert.Equal(expected.ShippingBin.Items, actual.ShippingBin.Items);
    }
}
