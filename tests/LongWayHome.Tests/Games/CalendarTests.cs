using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The calendar: <see cref="GameDate.Next"/> and the Night's date step (BR-G1, #38).</summary>
public class CalendarTests
{
    public static TheoryData<GameDate, GameDate> NextDays => new()
    {
        { new GameDate(1, Season.Spring, 1), new GameDate(1, Season.Spring, 2) },
        { new GameDate(1, Season.Spring, 28), new GameDate(1, Season.Summer, 1) },
        { new GameDate(1, Season.Summer, 28), new GameDate(1, Season.Fall, 1) },
        { new GameDate(1, Season.Fall, 28), new GameDate(1, Season.Winter, 1) },
        { new GameDate(1, Season.Winter, 28), new GameDate(2, Season.Spring, 1) },
    };

    [Theory]
    [MemberData(nameof(NextDays))]
    public void AC1_next_moves_to_the_following_day_season_and_year(GameDate date, GameDate expected)
    {
        Assert.Equal(expected, date.Next());
    }

    [Theory]
    [InlineData(Season.Spring, 13, Season.Spring, 14)]
    [InlineData(Season.Summer, 27, Season.Summer, 28)]
    [InlineData(Season.Winter, 1, Season.Winter, 2)]
    public void BR_G1_within_a_season_next_is_day_plus_one_in_the_same_season_and_year(
        Season season, int day, Season expectedSeason, int expectedDay)
    {
        Assert.Equal(new GameDate(3, expectedSeason, expectedDay), new GameDate(3, season, day).Next());
    }

    [Fact]
    public void BR_G1_winter_28_is_followed_by_spring_1_of_the_next_year()
    {
        Assert.Equal(new GameDate(4, Season.Spring, 1), new GameDate(3, Season.Winter, 28).Next());
    }

    [Fact]
    public void BR_G1_next_leaves_the_date_it_is_called_on_unchanged()
    {
        var date = new GameDate(1, Season.Spring, 28);

        _ = date.Next();

        Assert.Equal(new GameDate(1, Season.Spring, 28), date);
    }

    [Fact]
    public void BR_G1_advance_date_moves_the_game_to_the_next_day()
    {
        var game = TestGames.OnDate(new GameDate(2, Season.Fall, 28), null);

        game.AdvanceDate();

        Assert.Equal(new GameDate(2, Season.Winter, 1), game.Date);
    }

    [Fact]
    public void AC2_one_night_advances_the_date_by_exactly_one_day()
    {
        var game = TestGames.WithEnergy(150);

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.Slept);

        Assert.Equal(new GameDate(1, Season.Spring, 2), game.Date);
    }

    [Fact]
    public void AC2_passing_out_also_advances_the_date_by_one_day()
    {
        var game = TestGames.OnDate(new GameDate(1, Season.Spring, 28), null);

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.PassedOut);

        Assert.Equal(new GameDate(1, Season.Summer, 1), game.Date);
    }

    [Fact]
    public void AC2_112_nights_from_spring_1_year_1_reach_spring_1_year_2_one_day_at_a_time()
    {
        var session = new GameSession(NewGameFactory.Create(TestGames.Shipped, TestGames.Now), TestGames.Shipped);
        Assert.Equal(new GameDate(1, Season.Spring, 1), session.Game.Date);

        for (var night = 0; night < 112; night++)
        {
            var before = session.Game.Date;

            Assert.True(session.Sleep().IsOk);

            Assert.Equal(before.Next(), session.Game.Date);
        }

        Assert.Equal(new GameDate(2, Season.Spring, 1), session.Game.Date);
    }

    [Fact]
    public void AC2_the_date_does_not_change_during_the_day()
    {
        var session = TestGames.NewSession();

        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds / 2.0);
        session.Game.Till(new GridPosition(3, 2), TestGames.Shipped.Tuning.ToolEnergyCost);

        Assert.Equal(new GameDate(1, Season.Spring, 1), session.Game.Date);
        Assert.True(session.Sleep().IsOk);
        Assert.Equal(new GameDate(1, Season.Spring, 2), session.Game.Date);
    }
}
