using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The canonical <see cref="AreaId"/> codes the area scenes' exits name (#136, canonical-values.md).</summary>
public class AreaCodesTests
{
    [Theory]
    [InlineData(AreaId.Farm, "farm")]
    [InlineData(AreaId.ForestPath, "forest_path")]
    [InlineData(AreaId.Town, "town")]
    [InlineData(AreaId.Lakeshore, "lakeshore")]
    [InlineData(AreaId.AcrossRiver, "across_river")]
    [InlineData(AreaId.Mine, "mine")]
    public void Every_area_round_trips_through_its_canonical_code(AreaId area, string code)
    {
        Assert.Equal(code, AreaCodes.Code(area));
        Assert.True(AreaCodes.TryParse(code, out var parsed));
        Assert.Equal(area, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Town")]
    [InlineData("forestpath")]
    [InlineData("ForestPath")]
    [InlineData(" town")]
    public void Anything_but_an_exact_code_is_not_an_area(string? code)
    {
        Assert.False(AreaCodes.TryParse(code, out var area));
        Assert.Equal(default, area);
    }

    [Fact]
    public void A_value_outside_the_enum_has_no_code()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AreaCodes.Code((AreaId)99));
    }

    [Fact]
    public void Only_the_farm_has_the_farm_plot()
    {
        var session = TestGames.NewSession();
        Assert.True(session.IsOnFarmPlot);

        foreach (var area in new[] { AreaId.ForestPath, AreaId.Town, AreaId.Lakeshore })
        {
            Assert.Equal(ActionResult.Ok, session.EnterArea(area));
            Assert.False(session.IsOnFarmPlot);
        }

        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Farm));
        Assert.True(session.IsOnFarmPlot);
    }

    [Fact]
    public void Farm_commands_change_no_tile_off_the_farm()
    {
        var session = TestGames.SessionWithEnergy(150);
        var tile = new LongWayHome.Domain.Farming.GridPosition(4, 3);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        var before = TestGames.FullSnapshot(session.Game);
        var noTarget = ActionResult.Rejected(Rejection.NoTarget);

        Assert.Equal(noTarget, session.Till(tile));
        Assert.Equal(noTarget, session.Water(tile));
        Assert.Equal(noTarget, session.Plant(tile, LongWayHome.Domain.Catalog.CropTypeId.Emberroot));
        Assert.Equal(noTarget, session.Harvest(tile));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));

        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Farm));
        Assert.Equal(ActionResult.Ok, session.Till(tile));
    }

    [Fact]
    public void The_nights_return_to_the_farm_raises_no_area_changed()
    {
        var session = TestGames.NewSession();
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        var changes = new List<AreaId>();
        session.AreaChanged += area => changes.Add(area);

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.Equal(AreaId.Farm, session.CurrentArea);
        Assert.Empty(changes);
    }
}
