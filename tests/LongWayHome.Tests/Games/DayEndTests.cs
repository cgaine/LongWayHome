using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Ending the Day (BR-G3): <see cref="GameSession.Sleep"/>, passing out in <see cref="GameSession.AdvanceClock"/>,
/// the DayEnded and NightCompleted events, and the next morning's energy (BR-P1, BR-P3, BR-P4).</summary>
public class DayEndTests
{
    private const int DayStart = 360;
    private const int DayEnd = 1560;

    private static readonly GridPosition Spot = new(3, 2);

    /// <summary>A session with a watered growing Planting, a dry one, a ripe one, a bare tilled Tile and a part-used can.</summary>
    private static GameSession BusySession(int energy)
    {
        var game = TestGames.WithCan(
            energy,
            TestGames.CanWith(7),
            TestGames.StartingSeeds(),
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: true),
            TestGames.TilledWith(new GridPosition(4, 2), TestGames.PlantingOf(CropTypeId.Dusktuber, 2), watered: false),
            TestGames.TilledWith(new GridPosition(5, 2), TestGames.Ripe(CropTypeId.Moonmelon), watered: true),
            TestGames.TilledWith(new GridPosition(6, 2), null, watered: true));
        game.Player.Inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 3);
        return new GameSession(game, TestGames.Shipped);
    }

    /// <summary>Everything BR-P4 says must not depend on how the Day ended: gold, can, date, clock, Tiles, inventory.</summary>
    private static (int Gold, int Charges, GameDate Date, int Clock, string Tiles, string Inventory) AllButEnergy(GameState game)
    {
        var full = TestGames.FullSnapshot(game);
        return (game.Player.Gold.Value, game.Player.WateringCan.Charges, game.Date, game.ClockMinutes, full.Tiles, full.Inventory);
    }

    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.DayEnded += reason => events.Add($"DayEnded:{reason}");
        session.NightCompleted += () => events.Add("NightCompleted");
        return events;
    }

    [Fact]
    public void AC1_sleep_at_14_00_starts_the_next_day_at_06_00_with_full_energy()
    {
        var session = TestGames.SessionWithEnergy(20);
        session.AdvanceClock(360);
        Assert.Equal(960, session.Game.ClockMinutes);

        var result = session.Sleep();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(DayStart, session.Game.ClockMinutes);
        Assert.Equal(DayEndReason.Slept, session.Game.LastDayEnd);
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Fact]
    public void AC2_reaching_02_00_passes_out_and_wakes_at_06_00_with_75_energy()
    {
        var session = TestGames.SessionWithEnergy(20);

        session.AdvanceClock(720);

        Assert.Equal(DayStart, session.Game.ClockMinutes);
        Assert.Equal(DayEndReason.PassedOut, session.Game.LastDayEnd);
        Assert.Equal(75, session.Game.Player.Energy);
    }

    [Fact]
    public void AC3_a_slept_and_a_passed_out_game_differ_only_in_energy_and_last_day_end()
    {
        var slept = BusySession(40);
        var passedOut = BusySession(40);

        slept.Sleep();
        passedOut.AdvanceClock(720);

        Assert.Equal(AllButEnergy(slept.Game), AllButEnergy(passedOut.Game));
        Assert.Equal(150, slept.Game.Player.Energy);
        Assert.Equal(75, passedOut.Game.Player.Energy);
        Assert.Equal(DayEndReason.Slept, slept.Game.LastDayEnd);
        Assert.Equal(DayEndReason.PassedOut, passedOut.Game.LastDayEnd);
    }

    [Fact]
    public void BR_P4_passing_out_changes_only_the_next_days_energy()
    {
        // Both reach 01:59 the same way; one sleeps then, the other stays up past 02:00 and passes out.
        var slept = BusySession(90);
        var passedOut = BusySession(90);
        slept.AdvanceClock(719.4);
        passedOut.AdvanceClock(719.4);
        Assert.Equal(DayEnd - 1, slept.Game.ClockMinutes);

        slept.Sleep();
        passedOut.AdvanceClock(1);

        Assert.Equal(AllButEnergy(slept.Game), AllButEnergy(passedOut.Game));
        Assert.Equal(TestGames.Shipped.Tuning.MaxEnergy, slept.Game.Player.Energy);
        Assert.Equal(TestGames.Shipped.Tuning.PassOutEnergy, passedOut.Game.Player.Energy);
    }

    [Fact]
    public void AC10_sleep_raises_day_ended_once_with_slept_then_night_completed_once()
    {
        var session = TestGames.SessionWithEnergy(60);
        var events = RecordEvents(session);

        session.Sleep();

        Assert.Equal(new[] { "DayEnded:Slept", "NightCompleted" }, events);
    }

    [Fact]
    public void AC10_passing_out_raises_day_ended_once_with_passed_out_then_night_completed_once()
    {
        var session = TestGames.SessionWithEnergy(60);
        var events = RecordEvents(session);

        session.AdvanceClock(10_000);

        Assert.Equal(new[] { "DayEnded:PassedOut", "NightCompleted" }, events);
    }

    [Fact]
    public void AC10_sleep_at_02_00_returns_too_late_and_changes_nothing()
    {
        var game = TestGames.WithTiles(
            30, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: true));
        game.AdvanceClock(DayEnd - DayStart, DayEnd); // The Game's own clamp: the clock sits at 02:00 without a pass-out.
        var session = new GameSession(game, TestGames.Shipped);
        var before = TestGames.FullSnapshot(game);
        var events = RecordEvents(session);

        var result = session.Sleep();

        Assert.Equal(ActionResult.Rejected(Rejection.TooLate), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
        Assert.Equal(DayEnd, game.ClockMinutes);
        Assert.Null(game.LastDayEnd);
        Assert.Empty(events);
    }

    [Fact]
    public void BR_G3_a_day_ends_exactly_once_however_far_past_02_00_the_clock_is_pushed()
    {
        var session = TestGames.SessionWithEnergy(60);
        var events = RecordEvents(session);

        session.AdvanceClock(double.PositiveInfinity);

        Assert.Equal(new[] { "DayEnded:PassedOut", "NightCompleted" }, events);
        Assert.Equal(DayStart, session.Game.ClockMinutes);
    }

    [Fact]
    public void BR_G3_the_leftover_seconds_of_the_pass_out_call_are_discarded()
    {
        var session = TestGames.SessionWithEnergy(60);

        session.AdvanceClock(721); // One real second (about 1.7 game minutes) past 02:00.
        Assert.Equal(DayStart, session.Game.ClockMinutes);

        session.AdvanceClock(0.3); // 0.5 game minutes: nothing was carried, so still 06:00.
        Assert.Equal(DayStart, session.Game.ClockMinutes);
    }

    [Fact]
    public void BR_G3_a_sleep_right_after_a_pass_out_ends_the_new_day()
    {
        var session = TestGames.SessionWithEnergy(60);
        session.AdvanceClock(720);
        var events = RecordEvents(session);

        var result = session.Sleep();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new[] { "DayEnded:Slept", "NightCompleted" }, events);
        Assert.Equal(DayEndReason.Slept, session.Game.LastDayEnd);
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(151)]
    public void BR_P1_start_day_outside_0_to_150_throws_and_keeps_the_energy(int energy)
    {
        var player = TestGames.WithEnergy(40).Player;

        Assert.Throws<ArgumentOutOfRangeException>(() => player.StartDay(energy));
        Assert.Equal(40, player.Energy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(75)]
    [InlineData(150)]
    public void BR_P1_start_day_sets_energy_within_0_to_150(int energy)
    {
        var player = TestGames.WithEnergy(40).Player;

        player.StartDay(energy);

        Assert.Equal(energy, player.Energy);
    }

    [Theory]
    [InlineData(DayEndReason.Slept, 120)]
    [InlineData(DayEndReason.PassedOut, 45)]
    public void BR_P3_the_next_mornings_energy_and_clock_come_from_the_tuning(DayEndReason reason, int expectedEnergy)
    {
        var tuning = TestGames.Shipped.Tuning with { MaxEnergy = 120, PassOutEnergy = 45, DayStartMinutes = 300 };
        var data = TestGames.Shipped with { Tuning = tuning };
        var game = TestGames.WithEnergy(10);

        NightProcessor.Run(game, data, reason);

        Assert.Equal(expectedEnergy, game.Player.Energy);
        Assert.Equal(300, game.ClockMinutes);
        Assert.Equal(reason, game.LastDayEnd);
    }

    [Fact]
    public void BR_P3_start_next_day_sets_last_day_end_energy_and_clock()
    {
        var game = TestGames.WithEnergy(10);
        game.AdvanceClock(500, DayEnd);

        game.StartNextDay(DayEndReason.PassedOut, 75, DayStart);

        Assert.Equal(DayEndReason.PassedOut, game.LastDayEnd);
        Assert.Equal(75, game.Player.Energy);
        Assert.Equal(DayStart, game.ClockMinutes);
    }
}
