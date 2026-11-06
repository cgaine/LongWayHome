using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Farming;

public class FarmPlotTests
{
    [Fact]
    public void BR_T1_farm_plot_is_12_wide_and_9_high()
    {
        Assert.Equal(12, FarmPlot.Width);
        Assert.Equal(9, FarmPlot.Height);
    }

    [Fact]
    public void BR_T1_farm_plot_has_one_tile_per_grid_position()
    {
        var plot = new FarmPlot();

        Assert.Equal(108, plot.Tiles.Count);
        Assert.Equal(108, plot.Tiles.Select(tile => tile.Position).Distinct().Count());
        Assert.All(plot.Tiles, tile =>
        {
            Assert.InRange(tile.Position.X, 0, 11);
            Assert.InRange(tile.Position.Y, 0, 8);
        });
    }

    [Fact]
    public void BR_T1_tiles_are_enumerated_row_major()
    {
        var plot = new FarmPlot();

        var expected =
            from y in Enumerable.Range(0, 9)
            from x in Enumerable.Range(0, 12)
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
    public void Indexer_returns_the_tile_at_that_position(int x, int y)
    {
        var plot = new FarmPlot();
        var position = new GridPosition(x, y);

        Assert.Equal(position, plot[position].Position);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(12, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 9)]
    public void AC7_grid_position_outside_plot_throws(int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPosition(x, y));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(12, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 9)]
    public void AC7_try_create_returns_false_outside_plot(int x, int y)
    {
        Assert.False(GridPosition.TryCreate(x, y, out _));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(11, 8)]
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
    public void AC7_grid_position_inside_plot_is_created(int x, int y)
    {
        var position = new GridPosition(x, y);

        Assert.Equal(x, position.X);
        Assert.Equal(y, position.Y);
    }
}
