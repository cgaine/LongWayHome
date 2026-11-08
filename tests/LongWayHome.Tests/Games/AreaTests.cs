using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>Which areas are open (#136, BR-A1): <see cref="LongWayHome.Domain.Games.Game.IsAreaOpen"/> on a new Game.</summary>
public class AreaTests
{
    [Theory]
    [InlineData(AreaId.Farm)]
    [InlineData(AreaId.ForestPath)]
    [InlineData(AreaId.Town)]
    [InlineData(AreaId.Lakeshore)]
    public void BR_A1_Start_areas_are_open_in_a_new_game(AreaId area)
    {
        var game = TestGames.WithEnergy(150);

        Assert.True(game.IsAreaOpen(area));
    }

    [Theory]
    [InlineData(AreaId.AcrossRiver)]
    [InlineData(AreaId.Mine)]
    public void AC2_Across_river_and_mine_are_closed_in_a_new_game(AreaId area)
    {
        var game = TestGames.WithEnergy(150);

        Assert.False(game.IsAreaOpen(area));
    }
}
