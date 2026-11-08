using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

public class GameDateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BR_G1_game_date_rejects_year_below_1(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(year, Season.Spring, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(-5)]
    public void BR_G1_game_date_rejects_day_outside_1_to_28(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(1, Season.Spring, day));
    }

    [Theory]
    [InlineData(1, Season.Spring, 1)]
    [InlineData(1, Season.Winter, 28)]
    [InlineData(7, Season.Fall, 14)]
    public void BR_G1_game_date_accepts_year_1_and_up_and_days_1_to_28(int year, Season season, int day)
    {
        var date = new GameDate(year, season, day);

        Assert.Equal(year, date.Year);
        Assert.Equal(season, date.Season);
        Assert.Equal(day, date.Day);
    }

    [Fact]
    public void BR_G1_game_dates_with_the_same_values_are_equal()
    {
        Assert.Equal(new GameDate(1, Season.Spring, 1), new GameDate(1, Season.Spring, 1));
    }

    // ---- #153: DaysSince, the day difference BR-N4's wait counts in ----

    [Theory]
    [InlineData(1, Season.Spring, 3, 1, Season.Spring, 3, 0)]
    [InlineData(1, Season.Spring, 4, 1, Season.Spring, 3, 1)]
    [InlineData(1, Season.Spring, 28, 1, Season.Spring, 1, 27)]
    [InlineData(1, Season.Summer, 1, 1, Season.Spring, 28, 1)]
    [InlineData(1, Season.Fall, 5, 1, Season.Spring, 1, 60)]
    [InlineData(2, Season.Spring, 1, 1, Season.Winter, 28, 1)]
    [InlineData(2, Season.Spring, 1, 1, Season.Spring, 1, 112)]
    [InlineData(3, Season.Summer, 2, 1, Season.Winter, 27, 143)]
    public void BR_G1_days_since_counts_whole_days_across_seasons_and_years(
        int year, Season season, int day, int otherYear, Season otherSeason, int otherDay, int expected)
    {
        var later = new GameDate(year, season, day);
        var earlier = new GameDate(otherYear, otherSeason, otherDay);

        Assert.Equal(expected, later.DaysSince(earlier));
    }

    [Theory]
    [InlineData(1, Season.Spring, 3, 1, Season.Spring, 4, -1)]
    [InlineData(1, Season.Winter, 28, 2, Season.Spring, 1, -1)]
    [InlineData(1, Season.Spring, 1, 2, Season.Spring, 1, -112)]
    public void BR_G1_days_since_a_later_date_is_negative(
        int year, Season season, int day, int otherYear, Season otherSeason, int otherDay, int expected)
    {
        Assert.Equal(expected, new GameDate(year, season, day).DaysSince(new GameDate(otherYear, otherSeason, otherDay)));
    }

    [Fact]
    public void BR_G1_days_since_agrees_with_stepping_next_a_whole_year_and_more()
    {
        var start = new GameDate(1, Season.Fall, 20);
        var date = start;
        for (var steps = 1; steps <= 130; steps++)
        {
            date = date.Next();
            Assert.Equal(steps, date.DaysSince(start));
        }
    }
}
