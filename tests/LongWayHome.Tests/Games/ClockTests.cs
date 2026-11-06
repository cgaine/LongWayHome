using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The day clock (BR-G2): <see cref="GameSession.AdvanceClock"/> and the Game root's clamp.</summary>
public class ClockTests
{
    private const int DayEnd = 1560;

    [Fact]
    public void BR_G2_a_full_day_of_720_real_seconds_ends_at_02_00()
    {
        var session = TestGames.NewSession();

        for (var i = 0; i < 720; i++)
        {
            session.AdvanceClock(1.0);
        }

        Assert.Equal(1560, session.Game.ClockMinutes);
    }

    [Fact]
    public void AC1_advancing_720_real_seconds_reaches_02_00()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(720);

        Assert.Equal(1560, session.Game.ClockMinutes);
    }

    [Fact]
    public void AC2_advancing_360_real_seconds_reaches_14_00()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(360);

        Assert.Equal(960, session.Game.ClockMinutes);
    }

    [Fact]
    public void AC3_43200_frames_of_one_sixtieth_second_end_at_exactly_02_00()
    {
        var session = TestGames.NewSession();

        for (var i = 0; i < 43_200; i++)
        {
            session.AdvanceClock(1.0 / 60);
        }

        Assert.Equal(1560, session.Game.ClockMinutes);
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
    public void AC4_the_clock_stops_at_the_day_end_and_raises_nothing_after()
    {
        var session = TestGames.NewSession();
        session.AdvanceClock(10_000);
        Assert.Equal(1560, session.Game.ClockMinutes);
        var raised = 0;
        session.ClockChanged += () => raised++;

        session.AdvanceClock(1);

        Assert.Equal(1560, session.Game.ClockMinutes);
        Assert.Equal(0, raised);
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
}
