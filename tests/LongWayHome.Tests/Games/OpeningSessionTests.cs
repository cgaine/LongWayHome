using LongWayHome.Application.Games;
using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Saves;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #139, application: <see cref="GameSession.GiveStarterSeeds"/>, <see cref="GameSession.CompleteOpening"/> and the
/// unfinished-Opening default when a Day ends (AC2, AC3, AC6, BR-I1, BR-P6, BR-G3, BR-G5).
/// </summary>
public class OpeningSessionTests
{
    private static readonly DateTimeOffset NightTime = new(2026, 11, 6, 23, 50, 0, TimeSpan.Zero);

    /// <summary>Real seconds of play that take the clock from 06:00 to 02:00.</summary>
    private static double WholeDaySeconds => TestGames.Shipped.Tuning.DayLengthRealSeconds;

    /// <summary>What the Game held at the moment each Night saved it.</summary>
    private sealed class CapturingSaveStore : ISaveStore
    {
        public List<(bool OpeningDone, int Emberroot, int Dusktuber)> Saved { get; } = [];

        public IReadOnlyList<SaveSlotInfo> List() => throw new InvalidOperationException("Not used by these tests.");

        public GameState Load(int slot) => throw new InvalidOperationException("Not used by these tests.");

        public void Save(int slot, GameState game) =>
            Saved.Add((
                game.OpeningDone,
                OpeningGames.SeedCount(game, CropTypeId.Emberroot),
                OpeningGames.SeedCount(game, CropTypeId.Dusktuber)));
    }

    private static GameSession SessionOver(GameState game, ISaveStore? store = null) =>
        new(game, TestGames.Shipped, new FixedRandom(99), store ?? new CapturingSaveStore(), 1, new FixedTimeProvider(NightTime));

    // ---- AC2: GiveStarterSeeds ----

    [Fact]
    public void AC2_GiveStarterSeeds_on_a_new_game_returns_ok_and_adds_10_emberroot_and_5_dusktuber()
    {
        var session = SessionOver(OpeningGames.New());

        var result = session.GiveStarterSeeds();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(
            [(ItemKind.Seed, CropTypeId.Emberroot, 10), (ItemKind.Seed, CropTypeId.Dusktuber, 5)],
            TestGames.Stacks(session.Game.Player.Backpack));
    }

    [Fact]
    public void AC2_BR_P6_GiveStarterSeeds_gives_the_tunings_starting_seeds()
    {
        var session = SessionOver(OpeningGames.New());

        session.GiveStarterSeeds();

        Assert.All(
            TestGames.Shipped.Tuning.StartingSeeds,
            stack => Assert.Equal(stack.Quantity, OpeningGames.SeedCount(session.Game, stack.CropTypeId)));
    }

    [Fact]
    public void AC2_GiveStarterSeeds_costs_no_energy()
    {
        var session = SessionOver(OpeningGames.New());
        var energy = session.Game.Player.Energy;

        session.GiveStarterSeeds();

        Assert.Equal(energy, session.Game.Player.Energy);
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Fact]
    public void AC2_GiveStarterSeeds_raises_InventoryChanged_once()
    {
        var session = SessionOver(OpeningGames.New());
        var raised = 0;
        session.InventoryChanged += () => raised++;

        session.GiveStarterSeeds();

        Assert.Equal(1, raised);
    }

    // ---- AC3: refused ----

    [Fact]
    public void AC3_A_second_GiveStarterSeeds_in_the_same_session_is_rejected_OpeningOver_and_adds_nothing()
    {
        var session = SessionOver(OpeningGames.New());
        Assert.Equal(ActionResult.Ok, session.GiveStarterSeeds());
        var before = TestGames.FullSnapshot(session.Game);
        var raised = 0;
        session.InventoryChanged += () => raised++;

        var result = session.GiveStarterSeeds();

        Assert.Equal(ActionResult.Rejected(Rejection.OpeningOver), result);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void AC3_GiveStarterSeeds_on_a_game_whose_opening_is_done_is_rejected_OpeningOver_and_adds_nothing()
    {
        var session = SessionOver(TestGames.WithEnergy(150));
        var before = TestGames.FullSnapshot(session.Game);
        var raised = 0;
        session.InventoryChanged += () => raised++;

        var result = session.GiveStarterSeeds();

        Assert.Equal(ActionResult.Rejected(Rejection.OpeningOver), result);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
        Assert.Equal(0, raised);
    }

    [Fact]
    public void AC3_GiveStarterSeeds_that_do_not_fit_is_rejected_BackpackFull_and_raises_nothing()
    {
        var session = SessionOver(OpeningGames.NotDone(TestGames.FullBackpack()));
        var before = TestGames.FullSnapshot(session.Game);
        var raised = 0;
        session.InventoryChanged += () => raised++;

        var result = session.GiveStarterSeeds();

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
        Assert.Equal(0, raised);
    }

    // ---- BR-I1: CompleteOpening ----

    [Fact]
    public void BR_I1_Session_CompleteOpening_marks_the_games_opening_done()
    {
        var session = SessionOver(OpeningGames.New());

        session.CompleteOpening();

        Assert.True(session.Game.OpeningDone);
    }

    [Fact]
    public void BR_I1_GiveStarterSeeds_after_CompleteOpening_is_rejected_OpeningOver()
    {
        var session = SessionOver(OpeningGames.New());
        session.CompleteOpening();

        Assert.Equal(ActionResult.Rejected(Rejection.OpeningOver), session.GiveStarterSeeds());
    }

    // ---- AC6: the unfinished-Opening default at the Day's end ----

    [Fact]
    public void AC6_Sleeping_with_the_opening_unfinished_saves_it_done_with_the_15_starter_seeds()
    {
        var store = new CapturingSaveStore();
        var session = SessionOver(OpeningGames.New(), store);

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.Equal([(true, 10, 5)], store.Saved);
    }

    [Fact]
    public void AC6_Passing_out_with_the_opening_unfinished_saves_it_done_with_the_15_starter_seeds()
    {
        var store = new CapturingSaveStore();
        var session = SessionOver(OpeningGames.New(), store);

        session.AdvanceClock(WholeDaySeconds);

        Assert.Equal(DayEndReason.PassedOut, session.Game.LastDayEnd);
        Assert.Equal([(true, 10, 5)], store.Saved);
    }

    [Fact]
    public void AC6_Seeds_already_given_this_session_are_not_given_again_when_the_day_ends()
    {
        var store = new CapturingSaveStore();
        var session = SessionOver(OpeningGames.New(), store);
        Assert.Equal(ActionResult.Ok, session.GiveStarterSeeds());

        session.Sleep();

        Assert.Equal([(true, 10, 5)], store.Saved);
    }

    [Fact]
    public void AC6_The_opening_is_completed_and_the_seeds_given_before_DayEnded_is_raised()
    {
        var session = SessionOver(OpeningGames.New());
        (bool OpeningDone, int Emberroot, int Dusktuber)? atDayEnd = null;
        session.DayEnded += _ => atDayEnd = (
            session.Game.OpeningDone,
            OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot),
            OpeningGames.SeedCount(session.Game, CropTypeId.Dusktuber));

        session.Sleep();

        Assert.Equal((true, 10, 5), atDayEnd);
    }

    [Fact]
    public void AC6_The_day_end_default_raises_InventoryChanged_for_the_seeds()
    {
        var session = SessionOver(OpeningGames.New());
        var raised = 0;
        session.InventoryChanged += () => raised++;

        session.Sleep();

        Assert.True(raised >= 1, "the Day's end gave the starter seeds without raising InventoryChanged");
    }

    [Fact]
    public void BR_I1_Ending_a_day_whose_opening_is_already_done_gives_no_seeds()
    {
        var store = new CapturingSaveStore();
        var session = SessionOver(TestGames.WithEnergy(150), store);

        session.Sleep();

        Assert.Equal([(true, 0, 0)], store.Saved);
    }

    [Fact]
    public void BR_P6_A_refused_sleep_leaves_the_opening_unfinished_and_gives_no_seeds()
    {
        var store = new CapturingSaveStore();
        var game = OpeningGames.NotDone();
        game.AdvanceClock(TestGames.Shipped.Tuning.DayEndMinutes - game.ClockMinutes, TestGames.Shipped.Tuning.DayEndMinutes);
        var session = SessionOver(game, store);

        Assert.Equal(ActionResult.Rejected(Rejection.TooLate), session.Sleep());

        Assert.False(session.Game.OpeningDone);
        Assert.Equal(0, OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot));
        Assert.Empty(store.Saved);
    }

    // ---- BR-G3, BR-G5: the Day still ends once and saves once ----

    [Fact]
    public void BR_G5_A_day_ended_mid_opening_saves_once_after_the_default()
    {
        var store = new CountingSaveStore();
        var session = SessionOver(OpeningGames.New(), store);

        session.Sleep();

        Assert.Equal([1], store.SavedSlots);
        Assert.True(session.Game.OpeningDone);
    }

    [Fact]
    public void BR_G3_Passing_out_mid_opening_ends_the_day_exactly_once()
    {
        var session = SessionOver(OpeningGames.New());
        var ended = new List<DayEndReason>();
        session.DayEnded += ended.Add;

        session.AdvanceClock(WholeDaySeconds);

        Assert.Equal([DayEndReason.PassedOut], ended);
        Assert.True(session.Game.OpeningDone);
    }
}
