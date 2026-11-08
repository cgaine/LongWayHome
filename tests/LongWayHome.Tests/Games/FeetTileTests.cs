using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Games;

/// <summary>The farmer's feet tile (#130, AC11): <see cref="FeetTile.Nearest"/> clamps a grid cell to the plot.</summary>
public class FeetTileTests
{
    [Theory]
    [InlineData(5, 4, 5, 4)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(FarmPlot.Width - 1, FarmPlot.Height - 1, FarmPlot.Width - 1, FarmPlot.Height - 1)]
    public void AC11_on_plot_cell_is_its_own_tile(int cellX, int cellY, int x, int y)
    {
        Assert.Equal(new GridPosition(x, y), FeetTile.Nearest(cellX, cellY));
    }

    [Theory]
    [InlineData(-3, 4, 0, 4)]
    [InlineData(FarmPlot.Width, 4, FarmPlot.Width - 1, 4)]
    [InlineData(FarmPlot.Width + 7, 2, FarmPlot.Width - 1, 2)]
    [InlineData(5, -1, 5, 0)]
    [InlineData(5, FarmPlot.Height, 5, FarmPlot.Height - 1)]
    [InlineData(-5, -5, 0, 0)]
    [InlineData(FarmPlot.Width + 20, FarmPlot.Height + 20, FarmPlot.Width - 1, FarmPlot.Height - 1)]
    [InlineData(int.MinValue, int.MaxValue, 0, FarmPlot.Height - 1)]
    public void AC11_off_plot_cell_clamps_to_nearest_plot_tile(int cellX, int cellY, int x, int y)
    {
        Assert.Equal(new GridPosition(x, y), FeetTile.Nearest(cellX, cellY));
    }

    // #150: the feet clamp to the grown 72 x 36 grid (0..71, 0..35).
    [Theory]
    [InlineData(71, 35, 71, 35)]
    [InlineData(35, 20, 35, 20)]
    [InlineData(72, 10, 71, 10)]
    [InlineData(40, 36, 40, 35)]
    [InlineData(500, 500, 71, 35)]
    [InlineData(36, -1, 36, 0)]
    public void Edge_feet_clamp_to_the_72_by_36_grid(int cellX, int cellY, int x, int y)
    {
        var feet = FeetTile.Nearest(cellX, cellY);

        Assert.Equal((x, y), (feet.X, feet.Y));
    }
}

/// <summary>Where each DroppedItem on one tile is drawn (#130, AC12): <see cref="DroppedItemLayout.Offset"/>.</summary>
public class DroppedItemLayoutTests
{
    [Fact]
    public void AC12_the_first_dropped_item_on_a_tile_sits_at_its_centre()
    {
        Assert.Equal((0f, 0f), DroppedItemLayout.Offset(0));
    }

    [Fact]
    public void AC12_several_dropped_items_on_one_tile_are_offset_so_each_shows_and_stay_on_the_tile()
    {
        var offsets = Enumerable.Range(0, 4).Select(DroppedItemLayout.Offset).ToList();

        Assert.Equal(4, offsets.Distinct().Count());
        Assert.All(offsets, offset =>
        {
            Assert.InRange(offset.X, -0.4f, 0.4f);
            Assert.InRange(offset.Z, -0.4f, 0.4f);
        });
    }

    [Fact]
    public void AC12_any_index_on_a_tile_stays_on_the_tile()
    {
        Assert.All(Enumerable.Range(0, 50).Select(DroppedItemLayout.Offset), offset =>
        {
            Assert.InRange(offset.X, -0.4f, 0.4f);
            Assert.InRange(offset.Z, -0.4f, 0.4f);
        });
    }
}
