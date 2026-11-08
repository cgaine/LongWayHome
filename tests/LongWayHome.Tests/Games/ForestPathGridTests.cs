using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #154: the forest path's mapping, beside <see cref="FarmGridLayout"/>. Tile (X, Y) is centred at world
/// (X − 7 + 0.5, Y − 20 + 0.5), so the 14 x 40 grid covers the strip x −7..7, z −20..20.
/// </summary>
public class ForestPathGridTests
{
    [Theory]
    [InlineData(0, 0, -6.5f, -19.5f)]
    [InlineData(7, 0, 0.5f, -19.5f)]
    [InlineData(13, 35, 6.5f, 15.5f)]
    [InlineData(10, 12, 3.5f, -7.5f)]
    public void BR_T1_ToWorld_centres_a_tile_on_the_strip(int x, int y, float worldX, float worldZ)
    {
        Assert.Equal((worldX, worldZ), ForestPathGrid.ToWorld(new GridPosition(x, y)));
    }

    [Fact]
    public void BR_T1_ToWorld_maps_the_south_corner_tile_to_the_strips_south_east_corner()
    {
        Assert.Equal((6.5f, 19.5f), ForestPathGrid.ToWorld(AreaGrid.Position(AreaId.ForestPath, 13, 39)));
    }

    [Theory]
    [InlineData(-6.5f, -19.5f, 0, 0)]
    [InlineData(-7f, -20f, 0, 0)]
    [InlineData(6.99f, 19.99f, 13, 39)]
    [InlineData(0f, -20f, 7, 0)]
    [InlineData(0f, 19.9f, 7, 39)]
    [InlineData(-7.01f, 0f, -1, 20)]
    [InlineData(7f, 20f, 14, 40)]
    public void BR_T1_CellAt_gives_the_cell_under_a_world_point_without_clamping(float worldX, float worldZ, int x, int y)
    {
        Assert.Equal((x, y), ForestPathGrid.CellAt(worldX, worldZ));
    }

    [Fact]
    public void BR_T1_every_forest_path_tile_round_trips_through_the_world()
    {
        for (var y = 0; y < 40; y++)
        {
            for (var x = 0; x < 14; x++)
            {
                var (worldX, worldZ) = ForestPathGrid.ToWorld(AreaGrid.Position(AreaId.ForestPath, x, y));

                Assert.Equal((x, y), ForestPathGrid.CellAt(worldX, worldZ));
            }
        }
    }
}
