using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #154: grid positions are per Area. <see cref="AreaGrid"/> gives each Area's size (the farm 72 x 36, BR-T1; the
/// forest path 14 x 40) and makes positions on it, while <see cref="GridPosition"/>'s own constructor and TryCreate
/// stay the farm's.
/// </summary>
public class AreaGridTests
{
    [Fact]
    public void BR_T1_the_farm_grid_is_72_by_36()
    {
        Assert.Equal((72, 36), AreaGrid.Size(AreaId.Farm));
    }

    [Fact]
    public void BR_T1_the_forest_path_grid_is_14_by_40()
    {
        Assert.Equal((14, 40), AreaGrid.Size(AreaId.ForestPath));
    }

    [Theory]
    [InlineData(AreaId.Town)]
    [InlineData(AreaId.Lakeshore)]
    [InlineData(AreaId.AcrossRiver)]
    [InlineData(AreaId.Mine)]
    public void BR_T1_every_other_area_keeps_the_farms_72_by_36_for_now(AreaId area)
    {
        Assert.Equal((72, 36), AreaGrid.Size(area));
    }

    [Theory]
    [InlineData(AreaId.ForestPath, 0, 0, true)]
    [InlineData(AreaId.ForestPath, 13, 39, true)]
    [InlineData(AreaId.ForestPath, 14, 0, false)]
    [InlineData(AreaId.ForestPath, 0, 40, false)]
    [InlineData(AreaId.ForestPath, -1, 5, false)]
    [InlineData(AreaId.ForestPath, 5, -1, false)]
    [InlineData(AreaId.ForestPath, 20, 10, false)]
    [InlineData(AreaId.Farm, 71, 35, true)]
    [InlineData(AreaId.Farm, 0, 36, false)]
    [InlineData(AreaId.Farm, 72, 0, false)]
    [InlineData(AreaId.Farm, 13, 38, false)]
    public void BR_T1_Contains_is_true_only_inside_the_areas_grid(AreaId area, int x, int y, bool expected)
    {
        Assert.Equal(expected, AreaGrid.Contains(area, x, y));
    }

    [Fact]
    public void BR_T1_TryCreate_makes_a_forest_path_position_beyond_the_farms_rows()
    {
        Assert.True(AreaGrid.TryCreate(AreaId.ForestPath, 13, 39, out var position));

        Assert.Equal((13, 39), (position.X, position.Y));
    }

    [Theory]
    [InlineData(AreaId.ForestPath, 14, 0)]
    [InlineData(AreaId.ForestPath, 0, 40)]
    [InlineData(AreaId.ForestPath, -1, 0)]
    [InlineData(AreaId.Farm, 0, 36)]
    [InlineData(AreaId.Farm, 72, 0)]
    public void BR_T1_TryCreate_refuses_a_position_outside_the_areas_grid(AreaId area, int x, int y)
    {
        Assert.False(AreaGrid.TryCreate(area, x, y, out var position));
        Assert.Equal(default, position);
    }

    [Fact]
    public void BR_T1_TryCreate_on_the_farm_matches_the_farm_constructor()
    {
        Assert.True(AreaGrid.TryCreate(AreaId.Farm, 71, 35, out var position));

        Assert.Equal(new GridPosition(71, 35), position);
    }

    [Fact]
    public void BR_T1_Position_on_the_forest_path_gives_the_tile_at_row_38()
    {
        var position = AreaGrid.Position(AreaId.ForestPath, 5, 38);

        Assert.Equal((5, 38), (position.X, position.Y));
    }

    [Fact]
    public void BR_T1_two_positions_made_for_the_same_tile_are_equal()
    {
        Assert.Equal(AreaGrid.Position(AreaId.ForestPath, 3, 39), AreaGrid.Position(AreaId.ForestPath, 3, 39));
        Assert.Equal(new GridPosition(3, 9), AreaGrid.Position(AreaId.ForestPath, 3, 9));
    }

    [Theory]
    [InlineData(AreaId.ForestPath, 14, 0)]
    [InlineData(AreaId.ForestPath, 0, 40)]
    [InlineData(AreaId.ForestPath, 0, -1)]
    [InlineData(AreaId.Farm, 0, 36)]
    [InlineData(AreaId.Farm, 72, 0)]
    public void BR_T1_Position_outside_the_areas_grid_throws(AreaId area, int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AreaGrid.Position(area, x, y));
    }

    [Fact]
    public void BR_T1_the_farm_GridPosition_constructor_still_refuses_row_36()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPosition(0, 36));
        Assert.False(GridPosition.TryCreate(5, 38, out _));
    }

    [Fact]
    public void BR_T1_the_farm_plot_still_has_exactly_72_by_36_tiles()
    {
        Assert.Equal(72 * 36, new FarmPlot().Tiles.Count);
    }
}
