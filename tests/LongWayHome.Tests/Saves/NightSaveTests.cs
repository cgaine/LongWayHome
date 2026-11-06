using LongWayHome.Application.Games;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>Saving at the end of the Night (BR-G5, REQ-11) through a GameSession built with a save store.</summary>
public sealed class NightSaveTests : IDisposable
{
    private static readonly DateTimeOffset NightTime = new(2026, 11, 6, 23, 45, 12, TimeSpan.Zero);

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path);

    private GameSession Session(GameState game, int slot = 1, int roll = 99) =>
        new(game, TestGames.Shipped, new FixedRandom(roll), Store(), slot, new FixedTimeProvider(NightTime));

    /// <summary>Real seconds that take the clock from 06:00 to 02:00, so the Player passes out.</summary>
    private static double WholeDaySeconds => TestGames.Shipped.Tuning.DayLengthRealSeconds;

    [Fact]
    public void AC1_night_saves_the_session_game_to_its_slot()
    {
        var session = Session(SaveGames.Rich(clock: 360, weather: Weather.Clear), slot: 2);

        Assert.True(session.Sleep().IsOk);

        Assert.True(File.Exists(_folder.SlotFile(2)));
        SavedState.AssertSame(session.Game, Store().Load(2));
    }

    [Fact]
    public void AC2_saved_game_has_the_mornings_state()
    {
        var start = SaveGames.Rich(clock: 900, weather: Weather.Clear);
        var bin = start.ShippingBin.Items.Sum(item => item.Quantity * TestGames.Crop(item.CropTypeId).SellPrice.Value);
        var session = Session(start, roll: 99);

        session.Sleep();
        var saved = Store().Load(1);

        Assert.Equal(new GameDate(2, Season.Summer, 15), saved.Date);
        Assert.Equal(Weather.Clear, saved.Weather);
        Assert.All(saved.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
        Assert.Equal(5, saved.FarmPlot[SaveGames.Growing].Planting!.DaysGrown);
        Assert.Equal(1234 + bin, saved.Player.Gold.Value);
        Assert.Empty(saved.ShippingBin.Items);
        Assert.Equal(Player.MaxEnergy, saved.Player.Energy);
        Assert.Equal(DayEndReason.Slept, saved.LastDayEnd);
    }

    [Fact]
    public void AC2_a_rain_night_saves_rain_watered_tiles_and_the_new_weather()
    {
        var session = Session(SaveGames.Rich(clock: 900, weather: Weather.Clear), roll: 0);

        session.Sleep();
        var saved = Store().Load(1);

        Assert.Equal(Weather.Rain, saved.Weather);
        Assert.True(saved.FarmPlot[SaveGames.DryEmpty].Watered);
        Assert.True(saved.FarmPlot[SaveGames.Growing].Watered);
        Assert.False(saved.FarmPlot[new GridPosition(0, 0)].Watered);
    }

    [Fact]
    public void BR_G5_save_runs_after_every_other_night_step()
    {
        var game = SaveGames.Rich(clock: 1200, weather: Weather.Clear);
        (GameDate Date, int Clock, DayEndReason? LastDayEnd, int Energy, int Gold, int BinItems, bool AnyWatered, int DaysGrown)? seen = null;
        var calls = 0;

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.PassedOut, new FixedRandom(99), saved =>
        {
            calls++;
            seen = (saved.Date, saved.ClockMinutes, saved.LastDayEnd, saved.Player.Energy, saved.Player.Gold.Value,
                saved.ShippingBin.Items.Count, saved.FarmPlot.Tiles.Any(tile => tile.Watered),
                saved.FarmPlot[SaveGames.Growing].Planting!.DaysGrown);
        });

        Assert.Equal(1, calls);
        Assert.Equal(
            (new GameDate(2, Season.Summer, 15), TestGames.Shipped.Tuning.DayStartMinutes, (DayEndReason?)DayEndReason.PassedOut,
                TestGames.Shipped.Tuning.PassOutEnergy, game.Player.Gold.Value, 0, false, 5),
            seen);
        Assert.True(game.Player.Gold.Value > 1234);
    }

    [Fact]
    public void BR_G5_the_save_step_receives_the_game_the_night_ran_on()
    {
        var game = SaveGames.Rich();
        GameState? saved = null;

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.Slept, new FixedRandom(99), g => saved = g);

        Assert.Same(game, saved);
    }

    [Fact]
    public void AC3_saved_and_loaded_clock_is_day_start()
    {
        var session = Session(SaveGames.Plain());
        session.AdvanceClock(100);
        Assert.NotEqual(360, session.Game.ClockMinutes);

        session.Sleep();

        Assert.Equal(360, _folder.ReadSlot(1)["clock"]!.GetValue<int>());
        var loaded = Store().Load(1);
        Assert.Equal(360, loaded.ClockMinutes);
        Assert.Equal(new GameDate(1, Season.Spring, 2), loaded.Date);
    }

    [Fact]
    public void AC4_saved_at_is_the_session_time()
    {
        var session = Session(SaveGames.Plain());

        session.Sleep();

        Assert.Equal(NightTime, Store().Load(1).SavedAt);
        Assert.Equal(NightTime, session.Game.SavedAt);
        Assert.Equal(NightTime, DateTimeOffset.Parse(_folder.ReadSlot(1)["savedAt"]!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AC5_sleep_saves()
    {
        var session = Session(SaveGames.Plain());

        session.Sleep();

        Assert.True(File.Exists(_folder.SlotFile(1)));
        Assert.Equal(DayEndReason.Slept, Store().Load(1).LastDayEnd);
    }

    [Fact]
    public void AC5_pass_out_saves()
    {
        var session = Session(SaveGames.Plain());

        session.AdvanceClock(WholeDaySeconds);

        Assert.True(File.Exists(_folder.SlotFile(1)));
        var loaded = Store().Load(1);
        Assert.Equal(DayEndReason.PassedOut, loaded.LastDayEnd);
        Assert.Equal(TestGames.Shipped.Tuning.PassOutEnergy, loaded.Player.Energy);
    }

    [Fact]
    public void AC5_nothing_is_saved_during_the_day()
    {
        var spot = new GridPosition(2, 2);
        var session = Session(SaveGames.Plain());

        session.Till(spot);
        session.Water(spot);
        session.Plant(spot, CropTypeId.Emberroot);
        session.AdvanceClock(WholeDaySeconds / 2);

        Assert.Empty(Directory.EnumerateFileSystemEntries(_folder.Path));
        Assert.All(Store().List(), info => Assert.Equal(SaveSlotState.Empty, info.State));
    }

    [Fact]
    public void BR_P4_pass_out_save_matches_sleep_save_except_energy()
    {
        var id = Guid.NewGuid();
        var sleeper = Session(SaveGames.Rich(id: id, clock: 360, weather: Weather.Clear), slot: 1);
        var passer = Session(SaveGames.Rich(id: id, clock: 360, weather: Weather.Clear), slot: 2);

        sleeper.Sleep();
        passer.AdvanceClock(WholeDaySeconds);
        var slept = Store().Load(1);
        var passedOut = Store().Load(2);

        Assert.Equal(DayEndReason.Slept, slept.LastDayEnd);
        Assert.Equal(DayEndReason.PassedOut, passedOut.LastDayEnd);
        Assert.NotEqual(slept.Player.Energy, passedOut.Player.Energy);
        Assert.Equal(slept.Player.Gold, passedOut.Player.Gold);
        Assert.Equal(slept.Player.Inventory.Stacks, passedOut.Player.Inventory.Stacks);
        Assert.Equal(slept.ShippingBin.Items, passedOut.ShippingBin.Items);
        Assert.Equal(TestGames.FullSnapshot(slept).Tiles, TestGames.FullSnapshot(passedOut).Tiles);
        Assert.Equal(slept.Date, passedOut.Date);
        Assert.Equal(slept.ClockMinutes, passedOut.ClockMinutes);
    }

    [Fact]
    public void BR_G3_each_day_end_saves_exactly_once()
    {
        var store = new CountingSaveStore();
        var session = new GameSession(
            SaveGames.Plain(), TestGames.Shipped, new FixedRandom(99), store, 3, new FixedTimeProvider(NightTime));

        session.Sleep();
        Assert.Equal([3], store.SavedSlots);

        session.AdvanceClock(WholeDaySeconds);
        Assert.Equal([3, 3], store.SavedSlots);
    }

    [Fact]
    public void BR_G3_a_refused_sleep_saves_nothing()
    {
        var store = new CountingSaveStore();
        var game = SaveGames.Rich(clock: TestGames.Shipped.Tuning.DayEndMinutes);
        var session = new GameSession(game, TestGames.Shipped, new FixedRandom(99), store, 1, new FixedTimeProvider(NightTime));

        Assert.False(session.Sleep().IsOk);

        Assert.Empty(store.SavedSlots);
    }

    public static TheoryData<Exception> StoreFailures => new()
    {
        new SaveStoreException(1, "disk full"),
        new IOException("disk full"),
        new UnauthorizedAccessException("disk full"),
    };

    [Theory]
    [MemberData(nameof(StoreFailures))]
    public void AC14_save_failure_does_not_stop_the_night(Exception failure)
    {
        var store = new ThrowingSaveStore(failure);
        var session = new GameSession(
            SaveGames.Plain(), TestGames.Shipped, new FixedRandom(99), store, 1, new FixedTimeProvider(NightTime));
        var events = new List<string>();
        session.SaveFailed += reason => events.Add("SaveFailed:" + reason);
        session.NightCompleted += () => events.Add("NightCompleted");

        var result = session.Sleep();

        Assert.True(result.IsOk);
        Assert.Equal(1, store.Saves);
        Assert.Equal(new GameDate(1, Season.Spring, 2), session.Game.Date);
        Assert.Equal(2, events.Count);
        Assert.StartsWith("SaveFailed:", events[0], StringComparison.Ordinal);
        Assert.Contains("disk full", events[0], StringComparison.Ordinal);
        Assert.Equal("NightCompleted", events[1]);
    }

    [Fact]
    public void Existing_session_constructor_still_runs_the_night_without_saving()
    {
        var session = new GameSession(SaveGames.Plain(), TestGames.Shipped);

        session.Sleep();

        Assert.Equal(new GameDate(1, Season.Spring, 2), session.Game.Date);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_folder.Path));
    }
}
