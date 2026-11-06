using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The day clock (BR-G2): <see cref="GameSession.AdvanceClock"/> and the Game root's clamp.</summary>
public class ClockTests
{
    private const int DayStart = 360;
    private const int DayEnd = 1560;

    /// <summary>Counts DayEnded raises; the day at 02:00 now ends in a pass-out (BR-G3, #36).</summary>
    private static Func<int> CountDayEnds(GameSession session)
    {
        var count = 0;
        session.DayEnded += _ => count++;
        return () => count;
    }

    private static void AssertPassedOut(GameSession session)
    {
        Assert.Equal(DayStart, session.Game.ClockMinutes);
        Assert.Equal(DayEndReason.PassedOut, session.Game.LastDayEnd);
        Assert.Equal(TestGames.Shipped.Tuning.PassOutEnergy, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_G2_a_full_day_of_720_real_seconds_ends_at_02_00_in_one_pass_out()
    {
        var session = TestGames.NewSession();
        var dayEnds = CountDayEnds(session);

        for (var i = 0; i < 720; i++)
        {
            session.AdvanceClock(1.0);
        }

        Assert.Equal(1, dayEnds());
        AssertPassedOut(session);
    }

    [Fact]
    public void AC1_advancing_720_real_seconds_reaches_02_00_and_passes_out()
    {
        var session = TestGames.NewSession();
        var dayEnds = CountDayEnds(session);

        session.AdvanceClock(720);

        Assert.Equal(1, dayEnds());
        AssertPassedOut(session);
    }

    [Fact]
    public void AC2_advancing_360_real_seconds_reaches_14_00()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(360);

        Assert.Equal(960, session.Game.ClockMinutes);
    }

    [Fact]
    public void AC3_43200_frames_of_one_sixtieth_second_end_at_exactly_02_00_in_one_pass_out()
    {
        var session = TestGames.NewSession();
        var dayEnds = CountDayEnds(session);

        for (var i = 0; i < 43_200; i++)
        {
            session.AdvanceClock(1.0 / 60);
        }

        // Exactly one Day ends: not short of 02:00 (no pass-out) and not into a second Day.
        Assert.Equal(1, dayEnds());
        AssertPassedOut(session);
    }

    [Fact]
    public void AC7_21600_frames_of_one_thirtieth_second_end_at_exactly_02_00_in_one_pass_out()
    {
        var session = TestGames.NewSession();
        var dayEnds = CountDayEnds(session);

        for (var i = 0; i < 21_600; i++)
        {
            session.AdvanceClock(1.0 / 30);
        }

        // A 720-s Day at the Steam Deck's 30 fps cap ends exactly one Day, as at 60 fps.
        Assert.Equal(1, dayEnds());
        AssertPassedOut(session);
    }

    [Fact]
    public void BR_G2_thirty_steps_of_one_thirtieth_second_advance_as_much_as_one_second()
    {
        var stepped = TestGames.NewSession();
        var whole = TestGames.NewSession();

        for (var i = 0; i < 30; i++)
        {
            stepped.AdvanceClock(1.0 / 30);
        }

        whole.AdvanceClock(1.0);

        Assert.Equal(whole.Game.ClockMinutes, stepped.Game.ClockMinutes);
    }

    [Fact]
    public void AC3_three_calls_of_0_3_seconds_move_the_clock_by_one_minute()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(0.3);
        session.AdvanceClock(0.3);
        session.AdvanceClock(0.3);

        Assert.Equal(361, session.Game.ClockMinutes);
    }

    [Fact]
    public void AC4_the_day_end_passes_out_and_the_next_call_runs_the_new_days_clock()
    {
        var session = TestGames.NewSession();
        session.AdvanceClock(10_000);
        AssertPassedOut(session);
        var dayEnds = CountDayEnds(session);
        var raised = 0;
        session.ClockChanged += () => raised++;

        session.AdvanceClock(1); // 1 real second is 1.67 game minutes of the new Day.

        Assert.Equal(DayStart + 1, session.Game.ClockMinutes);
        Assert.Equal(1, raised);
        Assert.Equal(0, dayEnds());
    }

    [Fact]
    public void AC5_advancing_zero_seconds_changes_nothing_and_raises_nothing()
    {
        var session = TestGames.NewSession();
        var raised = 0;
        session.ClockChanged += () => raised++;

        session.AdvanceClock(0);

        Assert.Equal(360, session.Game.ClockMinutes);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void AC5_advancing_negative_seconds_throws()
    {
        var session = TestGames.NewSession();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceClock(-1));
    }

    [Fact]
    public void AC6_clock_changed_is_raised_once_by_a_call_that_moves_a_minute_or_more()
    {
        var session = TestGames.NewSession();
        var raised = 0;
        session.ClockChanged += () => raised++;

        session.AdvanceClock(1.0);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void AC6_clock_changed_is_not_raised_by_a_call_that_moves_less_than_a_minute()
    {
        var session = TestGames.NewSession();
        var raised = 0;
        session.ClockChanged += () => raised++;

        session.AdvanceClock(0.3);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Game_AdvanceClock_adds_minutes_and_clamps_at_day_end()
    {
        var game = TestGames.WithEnergy(150);

        game.AdvanceClock(100, DayEnd);
        Assert.Equal(460, game.ClockMinutes);

        game.AdvanceClock(5000, DayEnd);
        Assert.Equal(1560, game.ClockMinutes);

        game.AdvanceClock(5000, DayEnd);
        Assert.Equal(1560, game.ClockMinutes);
    }

    [Fact]
    public void Game_AdvanceClock_negative_minutes_throws()
    {
        var game = TestGames.WithEnergy(150);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.AdvanceClock(-1, DayEnd));
    }

    [Fact]
    public void BR_G2_a_long_frame_of_5_seconds_advances_8_minutes()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(5);

        Assert.Equal(368, session.Game.ClockMinutes);
    }

    [Fact]
    public void BR_G2_the_part_minute_left_after_a_move_is_carried_to_the_next_call()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(0.9); // 1.5 game minutes: 1 now, 0.5 carried.
        Assert.Equal(361, session.Game.ClockMinutes);

        session.AdvanceClock(0.3); // 0.5 more: the carried 0.5 makes a whole minute.
        Assert.Equal(362, session.Game.ClockMinutes);
    }

    [Fact]
    public void BR_G2_reaching_the_day_end_raises_night_completed_once_instead_of_clock_changed()
    {
        var session = TestGames.NewSession();
        var raised = 0;
        var nights = 0;
        session.ClockChanged += () => raised++;
        session.NightCompleted += () => nights++;

        session.AdvanceClock(10_000);

        AssertPassedOut(session);
        Assert.Equal(0, raised);
        Assert.Equal(1, nights);
    }

    [Fact]
    public void BR_G2_an_infinite_step_ends_the_day_in_one_pass_out()
    {
        var session = TestGames.NewSession();
        var dayEnds = CountDayEnds(session);

        session.AdvanceClock(double.PositiveInfinity);

        Assert.Equal(1, dayEnds());
        AssertPassedOut(session);
    }

    [Fact]
    public void AdvanceClock_NaN_seconds_throws()
    {
        var session = TestGames.NewSession();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.AdvanceClock(double.NaN));
        Assert.Equal(360, session.Game.ClockMinutes);
    }

    [Fact]
    public void BR_G2_the_rate_comes_from_the_tuning()
    {
        var tuning = TestGames.Shipped.Tuning with { DayLengthRealSeconds = 1200 };
        var session = new GameSession(
            TestGames.WithEnergy(150),
            TestGames.Shipped with { Tuning = tuning });

        session.AdvanceClock(60); // 1200 minutes over 1200 s: 1 game minute per second.

        Assert.Equal(420, session.Game.ClockMinutes);
    }

    [Fact]
    public void Game_AdvanceClock_zero_minutes_changes_nothing()
    {
        var game = TestGames.WithEnergy(150);

        game.AdvanceClock(0, DayEnd);

        Assert.Equal(360, game.ClockMinutes);
    }

    [Fact]
    public void Game_AdvanceClock_the_largest_step_does_not_overflow_past_day_end()
    {
        var game = TestGames.WithEnergy(150);
        game.AdvanceClock(1000, DayEnd);

        game.AdvanceClock(int.MaxValue, DayEnd);

        Assert.Equal(1560, game.ClockMinutes);
    }
}
