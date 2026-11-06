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
}
