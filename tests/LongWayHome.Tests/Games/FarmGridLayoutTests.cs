using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #150: the grid's world mapping. Tile (X, Y) is centred at world (X − 36 + 0.5, Y − 4.5 + 0.5), so the old plot's
/// block (X 30–41, Y 0–8) stays where the 12 x 9 plot was and the grid covers x −36..36, z −4.5..31.5.
/// </summary>
public class FarmGridLayoutTests
{
    private const float Eps = 1e-4f;

    private static GridPosition At(int x, int y)
    {
        Assert.True(GridPosition.TryCreate(x, y, out var position), $"({x}, {y}) is not on the Farm plot");
        return position;
    }

    private static void AssertWorld((float X, float Z) expected, (float X, float Z) actual)
    {
        Assert.True(
            MathF.Abs(expected.X - actual.X) <= Eps && MathF.Abs(expected.Z - actual.Z) <= Eps,
            $"expected ({expected.X}, {expected.Z}), was ({actual.X}, {actual.Z})");
    }

    [Fact]
    public void AC4_to_world_puts_the_old_plots_north_west_corner_tile_where_it_was()
    {
        AssertWorld((-5.5f, -4f), FarmGridLayout.ToWorld(At(30, 0)));
    }

    [Fact]
    public void AC4_to_world_puts_the_old_plots_south_east_corner_tile_where_it_was()
    {
        AssertWorld((5.5f, 4f), FarmGridLayout.ToWorld(At(41, 8)));
    }

    [Fact]
    public void AC4_to_world_puts_the_grids_corners_at_its_world_edges()
    {
        AssertWorld((-35.5f, -4f), FarmGridLayout.ToWorld(new GridPosition(0, 0)));
        AssertWorld((35.5f, 31f), FarmGridLayout.ToWorld(At(71, 35)));
    }

    [Fact]
    public void AC4_cell_at_inverts_to_world_for_every_tile_centre()
    {
        var plot = new FarmPlot();
        Assert.Equal(2592, plot.Tiles.Count);

        foreach (var tile in plot.Tiles)
        {
            var (x, z) = FarmGridLayout.ToWorld(tile.Position);
            Assert.Equal((tile.Position.X, tile.Position.Y), FarmGridLayout.CellAt(x, z));
        }
    }

    [Theory]
    [InlineData(-35.99f, -4.49f, 0, 0)]
    [InlineData(35.99f, 31.49f, 71, 35)]
    [InlineData(0f, 0f, 36, 4)]
    [InlineData(-6f, -4.5f, 30, 0)]
    public void AC4_cell_at_floors_any_point_inside_a_tile(float x, float z, int cellX, int cellY)
    {
        Assert.Equal((cellX, cellY), FarmGridLayout.CellAt(x, z));
    }

    [Theory]
    [InlineData(0f, -7f)] // the farmhouse door
    [InlineData(10f, -9f)] // the moved pond
    [InlineData(-20f, -4.6f)] // just north of row 0
    [InlineData(-36.2f, 10f)] // west of the grid
    [InlineData(36.2f, 10f)] // east of the grid, on the river bank
    [InlineData(0f, 31.6f)] // south of the grid, at the exit
    public void Edge_north_strip_is_off_the_grid(float x, float z)
    {
        var (cellX, cellY) = FarmGridLayout.CellAt(x, z);

        Assert.False(GridPosition.TryCreate(cellX, cellY, out _), $"({x}, {z}) maps to ({cellX}, {cellY}), on the plot");
    }
}
