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

/// <summary>Starting a new Game in a save slot and loading one through <see cref="GameStarter"/> (#53, BR-G1, BR-G5, BR-P6).</summary>
public sealed class GameStarterTests : IDisposable
{
    private static readonly DateTimeOffset StartTime = new(2026, 11, 6, 18, 5, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NightTime = new(2026, 11, 6, 23, 50, 0, TimeSpan.Zero);

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path);

    private static int DayStart => TestGames.Shipped.Tuning.DayStartMinutes;

    private GameSession StartNew(int slot, DateTimeOffset? at = null) =>
        GameStarter.StartNew(TestGames.Shipped, Store(), slot, new FixedRandom(99), new FixedTimeProvider(at ?? StartTime));

    private GameSession Load(int slot) =>
        GameStarter.Load(TestGames.Shipped, Store(), slot, new FixedRandom(99), new FixedTimeProvider(NightTime));

    [Fact]
    public void AC4_new_game_writes_its_slot_at_once()
    {
        var session = StartNew(2);

        var slots = Store().List();
        Assert.Equal(SaveSlotState.Empty, slots[0].State);
        Assert.Equal(SaveSlotState.Saved, slots[1].State);
        Assert.Equal(SaveSlotState.Empty, slots[2].State);
        Assert.True(File.Exists(_folder.SlotFile(2)));
        SavedState.AssertSame(session.Game, Store().Load(2));
    }

    [Fact]
    public void AC4_new_game_slot_lists_the_starting_date_and_gold()
    {
        StartNew(1);

        var slot = Store().List()[0];
        Assert.Equal(SaveSlotState.Saved, slot.State);
        Assert.Equal(new GameDate(1, Season.Spring, 1), slot.Date);
        Assert.Equal(100, slot.Gold);
        Assert.Equal(StartTime, slot.SavedAt);
    }

    [Fact]
    public void AC4_new_game_session_starts_a_fresh_game()
    {
        var session = StartNew(1);

        Assert.Same(TestGames.Shipped, session.Data);
        Assert.Equal(new GameDate(1, Season.Spring, 1), session.Game.Date);
        Assert.Equal(DayStart, session.Game.ClockMinutes);
        Assert.Equal(StartTime, session.Game.CreatedAt);
        Assert.All(session.Game.FarmPlot.Tiles, tile => Assert.Equal(TileState.Grass, tile.State));
    }

    [Fact]
    public void AC4_new_game_overwrites_a_saved_slot()
    {
        Store().Save(1, SaveGames.Rich(clock: 360));

        var session = StartNew(1);

        var loaded = Store().Load(1);
        Assert.Equal(session.Game.Id, loaded.Id);
        Assert.Equal(new GameDate(1, Season.Spring, 1), loaded.Date);
        Assert.Equal(100, loaded.Player.Gold.Value);
    }

    [Fact]
    public void BR_G1_new_game_starts_on_year_1_spring_1_at_day_start()
    {
        StartNew(1);

        var loaded = Store().Load(1);
        Assert.Equal(new GameDate(1, Season.Spring, 1), loaded.Date);
        Assert.Equal(360, DayStart);
        Assert.Equal(DayStart, loaded.ClockMinutes);
        Assert.Null(loaded.LastDayEnd);
    }

    [Fact]
    public void BR_P6_new_game_slot_holds_the_starting_player()
    {
        var tuning = TestGames.Shipped.Tuning;

        StartNew(3);

        var player = Store().Load(3).Player;
        Assert.Equal(100, player.Gold.Value);
        Assert.Equal(tuning.StartingGold, player.Gold.Value);
        Assert.Equal(150, player.Energy);
        Assert.Equal(tuning.MaxEnergy, player.Energy);
        Assert.Equal(ToolTier.Basic, player.Backpack.TierOf(ToolId.Hoe));
        Assert.Equal(ToolTier.Basic, player.Backpack.TierOf(ToolId.WateringCan));
        Assert.Equal(20, player.WateringCan.Capacity);
        Assert.Equal(tuning.CanCapacity, player.WateringCan.Capacity);
        Assert.Equal(player.WateringCan.Capacity, player.WateringCan.Charges);
        Assert.False(player.WateringCan.Upgraded);
        Assert.Equal(10, player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal(5, player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
        Assert.Equal(TestGames.Describe(TestGames.StartingSeeds()), TestGames.Describe(player.Backpack));
    }

    [Fact]
    public void AC6_load_resumes_the_saved_game()
    {
        var saved = SaveGames.Rich(clock: DayStart, weather: Weather.Rain);
        Store().Save(2, saved);

        var session = Load(2);

        SavedState.AssertSame(saved, session.Game);
        Assert.Same(TestGames.Shipped, session.Data);
    }

    [Fact]
    public void AC6_loaded_game_is_on_the_saved_day_at_day_start_with_the_saved_state()
    {
        Store().Save(1, SaveGames.Rich(gold: 4321, date: new GameDate(3, Season.Fall, 20), clock: DayStart, weather: Weather.Rain));

        var game = Load(1).Game;

        Assert.Equal(new GameDate(3, Season.Fall, 20), game.Date);
        Assert.Equal(DayStart, game.ClockMinutes);
        Assert.Equal(Weather.Rain, game.Weather);
        Assert.Equal(4321, game.Player.Gold.Value);
        Assert.Equal(4, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Sunbell)));
        Assert.Equal(7, game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
        Assert.Equal(13, game.Player.WateringCan.Charges);
        Assert.Equal(40, game.Player.WateringCan.Capacity);
        Assert.Equal(TileState.Tilled, game.FarmPlot[SaveGames.DryEmpty].State);
        Assert.Equal(
            new Planting(CropTypeId.Starcorn, new GameDate(2, Season.Summer, 9), 4),
            game.FarmPlot[SaveGames.Growing].Planting);
    }

    [Fact]
    public void BR_G5_a_game_saved_after_a_night_loads_at_day_start_of_the_saved_day()
    {
        var session = StartNew(1);
        session.AdvanceClock(100);
        Assert.True(session.Till(new GridPosition(0, 0)).IsOk);
        Assert.True(session.Sleep().IsOk);

        var loaded = Load(1).Game;

        Assert.Equal(new GameDate(1, Season.Spring, 2), loaded.Date);
        Assert.Equal(DayStart, loaded.ClockMinutes);
        Assert.Equal(TileState.Tilled, loaded.FarmPlot[new GridPosition(0, 0)].State);
        Assert.Equal(DayEndReason.Slept, loaded.LastDayEnd);
    }

    [Fact]
    public void BR_G5_an_unslept_day_keeps_no_progress()
    {
        var session = StartNew(1);
        session.AdvanceClock(100);
        Assert.True(session.Till(new GridPosition(0, 0)).IsOk);

        var loaded = Load(1).Game;

        Assert.Equal(new GameDate(1, Season.Spring, 1), loaded.Date);
        Assert.Equal(DayStart, loaded.ClockMinutes);
        Assert.Equal(TileState.Grass, loaded.FarmPlot[new GridPosition(0, 0)].State);
        Assert.Equal(Player.MaxEnergy, loaded.Player.Energy);
    }

    [Fact]
    public void AC7_sleeping_in_a_new_game_in_slot_2_saves_slot_2_only()
    {
        Store().Save(1, SaveGames.Rich(clock: DayStart));
        Store().Save(3, SaveGames.Plain(gold: 777));
        var slot1 = File.ReadAllBytes(_folder.SlotFile(1));
        var slot3 = File.ReadAllBytes(_folder.SlotFile(3));
        var session = StartNew(2);

        Assert.True(session.Sleep().IsOk);

        Assert.Equal(new GameDate(1, Season.Spring, 2), Store().Load(2).Date);
        Assert.Equal(slot1, File.ReadAllBytes(_folder.SlotFile(1)));
        Assert.Equal(slot3, File.ReadAllBytes(_folder.SlotFile(3)));
    }

    [Fact]
    public void AC7_sleeping_in_a_game_loaded_from_slot_2_saves_slot_2_only()
    {
        Store().Save(1, SaveGames.Plain(gold: 111));
        Store().Save(2, SaveGames.Rich(clock: DayStart, weather: Weather.Clear));
        Store().Save(3, SaveGames.Plain(gold: 333));
        var slot1 = File.ReadAllBytes(_folder.SlotFile(1));
        var slot3 = File.ReadAllBytes(_folder.SlotFile(3));
        var session = Load(2);

        Assert.True(session.Sleep().IsOk);

        var saved = Store().Load(2);
        Assert.Equal(new GameDate(2, Season.Summer, 15), saved.Date);
        Assert.Equal(NightTime, saved.SavedAt);
        Assert.Equal(slot1, File.ReadAllBytes(_folder.SlotFile(1)));
        Assert.Equal(slot3, File.ReadAllBytes(_folder.SlotFile(3)));
    }

    [Fact]
    public void AC9_loading_an_empty_slot_throws_save_store_exception()
    {
        var error = Assert.Throws<SaveStoreException>(() => Load(2));

        Assert.Equal(2, error.Slot);
    }

    [Fact]
    public void AC9_loading_an_unreadable_slot_throws_save_store_exception()
    {
        File.WriteAllText(_folder.SlotFile(3), "garbage");
        Assert.Equal(SaveSlotState.Unreadable, Store().List()[2].State);

        var error = Assert.Throws<SaveStoreException>(() => Load(3));

        Assert.Equal(3, error.Slot);
    }

    // ---- #139: the Opening ----

    [Fact]
    public void AC4_A_new_game_saved_at_once_loads_with_the_opening_not_done()
    {
        StartNew(1);

        Assert.False(Load(1).Game.OpeningDone);
    }

    [Fact]
    public void AC4_BR_G5_Quitting_mid_opening_and_loading_plays_the_opening_again_with_no_seeds()
    {
        var session = StartNew(1);
        Assert.True(session.GiveStarterSeeds().IsOk);
        session.CompleteOpening();

        var loaded = Load(1).Game;

        Assert.False(loaded.OpeningDone);
        Assert.Equal(0, loaded.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal(0, loaded.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void AC6_Sleeping_on_the_first_day_without_finishing_the_opening_loads_it_done_with_the_15_seeds()
    {
        var session = StartNew(1);

        Assert.True(session.Sleep().IsOk);

        var loaded = Load(1).Game;
        Assert.True(loaded.OpeningDone);
        Assert.Equal(10, loaded.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal(5, loaded.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void A_new_game_whose_save_fails_throws_the_store_error()
    {
        var store = new ThrowingSaveStore(new IOException("disk full"));

        Assert.Throws<IOException>(() =>
            GameStarter.StartNew(TestGames.Shipped, store, 1, new FixedRandom(99), new FixedTimeProvider(StartTime)));
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public void Start_and_load_refuse_a_null_store()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GameStarter.StartNew(TestGames.Shipped, null!, 1, new FixedRandom(99), new FixedTimeProvider(StartTime)));
        Assert.Throws<ArgumentNullException>(() =>
            GameStarter.StartNew(TestGames.Shipped, Store(), 1, new FixedRandom(99), null!));
        Assert.Throws<ArgumentNullException>(() =>
            GameStarter.Load(TestGames.Shipped, null!, 1, new FixedRandom(99), new FixedTimeProvider(StartTime)));
    }
}
