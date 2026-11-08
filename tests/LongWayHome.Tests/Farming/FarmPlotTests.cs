using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Farming;

public class FarmPlotTests
{
    // ---- #150: the plot grows to the 72 x 36 meadow ----

    [Fact]
    public void AC1_new_plot_has_2592_grass_unwatered_tiles_one_per_position()
    {
        var plot = new FarmPlot();

        Assert.Equal(2592, plot.Tiles.Count);
        var expected =
            from y in Enumerable.Range(0, 36)
            from x in Enumerable.Range(0, 72)
            select (x, y);
        Assert.Equal(expected.Order(), plot.Tiles.Select(tile => (tile.Position.X, tile.Position.Y)).Order());
        Assert.All(plot.Tiles, tile =>
        {
            Assert.Equal(TileState.Grass, tile.State);
            Assert.False(tile.Watered);
            Assert.Null(tile.Planting);
        });
    }

    [Fact]
    public void BR_T1_plot_is_72_by_36_with_phase1_block_at_30_0()
    {
        Assert.Equal(72, FarmPlot.Width);
        Assert.Equal(36, FarmPlot.Height);
        Assert.Equal(new PlotBlock(30, 0, 12, 9), FarmPlot.Phase1Block);
    }

    [Fact]
    public void AC2_grid_position_bounds_are_0_71_and_0_35()
    {
        Assert.True(GridPosition.TryCreate(71, 35, out var corner), "(71, 35) is refused");
        Assert.Equal((71, 35), (corner.X, corner.Y));
        Assert.False(GridPosition.TryCreate(72, 0, out _), "(72, 0) is accepted");
        Assert.False(GridPosition.TryCreate(0, 36, out _), "(0, 36) is accepted");
    }

    // ---- BR-T1 ----

    [Fact]
    public void BR_T1_farm_plot_is_72_wide_and_36_high()
    {
        Assert.Equal(72, FarmPlot.Width);
        Assert.Equal(36, FarmPlot.Height);
    }

    [Fact]
    public void BR_T1_farm_plot_has_one_tile_per_grid_position()
    {
        var plot = new FarmPlot();

        Assert.Equal(2592, plot.Tiles.Count);
        Assert.Equal(2592, plot.Tiles.Select(tile => tile.Position).Distinct().Count());
        Assert.All(plot.Tiles, tile =>
        {
            Assert.InRange(tile.Position.X, 0, 71);
            Assert.InRange(tile.Position.Y, 0, 35);
        });
    }

    [Fact]
    public void BR_T1_tiles_are_enumerated_row_major()
    {
        var plot = new FarmPlot();

        var expected =
            from y in Enumerable.Range(0, 36)
            from x in Enumerable.Range(0, 72)
            select (x, y);
        Assert.Equal(expected, plot.Tiles.Select(tile => (tile.Position.X, tile.Position.Y)));
    }

    [Fact]
    public void BR_T1_new_farm_plot_tiles_are_grass_and_unwatered()
    {
        var plot = new FarmPlot();

        Assert.All(plot.Tiles, tile =>
        {
            Assert.Equal(TileState.Grass, tile.State);
            Assert.False(tile.Watered);
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(11, 8)]
    [InlineData(4, 7)]
    [InlineData(71, 35)]
    [InlineData(35, 20)]
    public void Indexer_returns_the_tile_at_that_position(int x, int y)
    {
        var plot = new FarmPlot();
        Assert.True(GridPosition.TryCreate(x, y, out var position), $"({x}, {y}) is off the plot");

        Assert.Equal(position, plot[position].Position);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(72, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 36)]
    public void AC7_grid_position_outside_plot_throws(int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPosition(x, y));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(72, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 36)]
    public void AC7_try_create_returns_false_outside_plot(int x, int y)
    {
        Assert.False(GridPosition.TryCreate(x, y, out _));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(11, 8)]
    [InlineData(71, 35)]
    public void AC7_try_create_returns_true_and_the_position_inside_plot(int x, int y)
    {
        var created = GridPosition.TryCreate(x, y, out var position);

        Assert.True(created);
        Assert.Equal(x, position.X);
        Assert.Equal(y, position.Y);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(11, 8)]
    [InlineData(71, 35)]
    public void AC7_grid_position_inside_plot_is_created(int x, int y)
    {
        Assert.True(GridPosition.TryCreate(x, y, out _), $"({x}, {y}) is off the plot");
        var position = new GridPosition(x, y);

        Assert.Equal(x, position.X);
        Assert.Equal(y, position.Y);
    }
}
