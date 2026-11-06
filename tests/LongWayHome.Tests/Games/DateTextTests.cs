using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The HUD date text: <see cref="DateText.For"/> (#38).</summary>
public class DateTextTests
{
    [Fact]
    public void AC11_a_new_game_shows_Spring_1_Year_1()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        Assert.Equal("Spring 1, Year 1", DateText.For(game.Date));
    }

    [Fact]
    public void AC11_winter_28_of_year_3_shows_Winter_28_Year_3()
    {
        Assert.Equal("Winter 28, Year 3", DateText.For(new GameDate(3, Season.Winter, 28)));
    }

    [Theory]
    [InlineData(Season.Spring, "Spring 2, Year 1")]
    [InlineData(Season.Summer, "Summer 2, Year 1")]
    [InlineData(Season.Fall, "Fall 2, Year 1")]
    [InlineData(Season.Winter, "Winter 2, Year 1")]
    public void AC11_each_season_shows_its_display_label(Season season, string expected)
    {
        Assert.Equal(expected, DateText.For(new GameDate(1, season, 2)));
    }

    [Fact]
    public void AC11_day_and_year_have_no_leading_zeros_or_padding()
    {
        Assert.Equal("Summer 14, Year 12", DateText.For(new GameDate(12, Season.Summer, 14)));
    }
}
