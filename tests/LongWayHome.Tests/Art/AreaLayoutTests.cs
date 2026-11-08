using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The area layouts (#136): the forest path's trees and winding path, the town's buildings and trees, and the
/// lakeshore's dock and rocks are deterministic, inside their area, and clear of the paths, exits, river and each other.
/// </summary>
public class AreaLayoutTests
{
    // ---- the shared scatter ----

    [Fact]
    public void Scatter_is_the_same_for_the_same_seed_and_differs_for_another()
    {
        var first = AreaScatter.Place(7, 12, (-5f, 5f, -5f, 5f), 1f, (1f, 1f), (_, _) => true);
        var again = AreaScatter.Place(7, 12, (-5f, 5f, -5f, 5f), 1f, (1f, 1f), (_, _) => true);
        var other = AreaScatter.Place(8, 12, (-5f, 5f, -5f, 5f), 1f, (1f, 1f), (_, _) => true);

        Assert.Equal(first, again);
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void Scatter_skips_props_that_find_no_clear_ground()
    {
        Assert.Empty(AreaScatter.Place(1, 5, (0f, 1f, 0f, 1f), 0.1f, (1f, 1f), (_, _) => false));
    }

    [Fact]
    public void Scatter_keeps_its_spacing_and_scale()
    {
        var props = AreaScatter.Place(3, 40, (-3f, 3f, -3f, 3f), 2f, (0.5f, 0.7f), (_, _) => true);

        Assert.NotEmpty(props);
        Assert.True(props.Count < 40, "a 6 x 6 m square can't fit 40 props 2 m apart");
        Assert.All(props, p => Assert.InRange(p.Scale, 0.5f, 0.7f));
        Assert.All(props, p => Assert.InRange(p.YawDegrees, 0f, 360f));
        for (var i = 0; i < props.Count; i++)
        {
            for (var j = i + 1; j < props.Count; j++)
            {
                Assert.True(AreaScatter.Distance(props[i].X, props[i].Z, props[j].X, props[j].Z) >= 2f);
            }
        }
    }

    [Fact]
    public void Scatter_needs_a_clear_test()
    {
        Assert.Throws<ArgumentNullException>(() => AreaScatter.Place(1, 1, (0f, 1f, 0f, 1f), 0f, (1f, 1f), null!));
    }

    // ---- the forest path ----

    [Fact]
    public void Forest_path_is_at_x_0_at_both_exits_and_stays_off_the_river()
    {
        Assert.Equal(0f, ForestPathLayout.PathCentreX(-ForestPathLayout.HalfLength), 3);
        Assert.Equal(0f, ForestPathLayout.PathCentreX(ForestPathLayout.HalfLength), 3);

        var centre = ForestPathLayout.PathCentre(0.5f);
        Assert.Equal(-ForestPathLayout.HalfLength, centre[0].Z);
        Assert.Equal(ForestPathLayout.HalfLength, centre[^1].Z);
        Assert.All(centre, p => Assert.InRange(
            p.X,
            -ForestPathLayout.HalfWidth + ForestPathLayout.PathHalfWidth,
            ForestPathLayout.RiverWestX - ForestPathLayout.PathHalfWidth));

        // It winds: west of the middle in the north half, east in the south half.
        Assert.True(ForestPathLayout.PathCentreX(-10f) < -1f);
        Assert.True(ForestPathLayout.PathCentreX(10f) > 1f);
    }

    [Fact]
    public void Forest_path_centre_needs_a_positive_step()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ForestPathLayout.PathCentre(0f));
    }

    [Fact]
    public void Forest_trees_are_deterministic_inside_the_area_and_clear_of_the_path_river_and_exits()
    {
        var trees = ForestPathLayout.Trees();

        Assert.Equal(trees, ForestPathLayout.Trees());
        Assert.True(trees.Count >= 20, $"only {trees.Count} trees on the forest path");
        foreach (var tree in trees)
        {
            Assert.InRange(tree.X, -ForestPathLayout.HalfWidth + 1f, ForestPathLayout.RiverWestX - 1f);
            Assert.InRange(tree.Z, -ForestPathLayout.HalfLength + 1f, ForestPathLayout.HalfLength - 1f);
            Assert.True(
                MathF.Abs(tree.X - ForestPathLayout.PathCentreX(tree.Z)) >= ForestPathLayout.PathHalfWidth + 1f,
                $"a tree at ({tree.X}, {tree.Z}) stands on the path");
            Assert.False(
                MathF.Abs(tree.Z) > ForestPathLayout.HalfLength - ForestPathLayout.ExitDepth && MathF.Abs(tree.X) < 3f,
                $"a tree at ({tree.X}, {tree.Z}) blocks an exit");
        }
    }

    [Fact]
    public void Forest_trees_keep_their_spacing()
    {
        var trees = ForestPathLayout.Trees();
        for (var i = 0; i < trees.Count; i++)
        {
            for (var j = i + 1; j < trees.Count; j++)
            {
                Assert.True(AreaScatter.Distance(trees[i].X, trees[i].Z, trees[j].X, trees[j].Z) >= ForestPathLayout.TreeSpacing);
            }
        }
    }

    // ---- the town ----

    [Fact]
    public void Town_has_the_store_the_carpenter_and_the_blacksmith_clear_of_the_road_square_and_river()
    {
        var kinds = TownLayout.Buildings.Select(b => b.Kind).ToList();
        Assert.Equal([TownBuildingKind.Store, TownBuildingKind.Carpenter, TownBuildingKind.Blacksmith], kinds);

        foreach (var building in TownLayout.Buildings)
        {
            var reach = building.WallRadius + TownLayout.EaveMargin;
            Assert.True(MathF.Abs(building.X) - reach > TownLayout.RoadHalfWidth, $"{building.Kind} is on the road");
            Assert.True(
                AreaScatter.Distance(building.X, building.Z, 0f, 0f) - reach > TownLayout.SquareRadius,
                $"{building.Kind} is on the square");
            Assert.True(building.X + reach < TownLayout.RiverWestX, $"{building.Kind} is in the river");
            Assert.True(building.X - reach > -TownLayout.HalfWidth, $"{building.Kind} is past the west edge");
            Assert.InRange(building.Z, -TownLayout.HalfDepth + reach, TownLayout.HalfDepth - reach);
        }
    }

    [Fact]
    public void Town_trees_are_deterministic_inside_the_town_and_clear_of_the_buildings_road_and_square()
    {
        var trees = TownLayout.Trees();

        Assert.Equal(trees, TownLayout.Trees());
        Assert.NotEmpty(trees);
        foreach (var tree in trees)
        {
            Assert.InRange(tree.X, -TownLayout.HalfWidth, TownLayout.RiverWestX - TownLayout.TreeClearance);
            Assert.InRange(tree.Z, -TownLayout.HalfDepth, TownLayout.HalfDepth);
            Assert.True(MathF.Abs(tree.X) >= TownLayout.RoadHalfWidth + TownLayout.TreeClearance);
            Assert.True(AreaScatter.Distance(tree.X, tree.Z, 0f, 0f) >= TownLayout.SquareRadius + TownLayout.TreeClearance);
            Assert.All(TownLayout.Buildings, b => Assert.True(
                AreaScatter.Distance(tree.X, tree.Z, b.X, b.Z) >= b.WallRadius + TownLayout.EaveMargin + TownLayout.TreeClearance,
                $"a tree at ({tree.X}, {tree.Z}) is under the {b.Kind}'s eaves"));
        }
    }

    // ---- the lakeshore ----

    [Fact]
    public void Dock_planks_run_from_the_shore_out_over_the_water()
    {
        var planks = LakeshoreLayout.DockPlanks();

        Assert.True(planks.Count > 10);
        Assert.All(planks, p => Assert.Equal((LakeshoreLayout.DockWestX + LakeshoreLayout.DockEastX) / 2f, p.X));
        Assert.True(planks[0].Z < LakeshoreLayout.WaterlineZ, "the dock doesn't start on the shore");
        Assert.True(planks[^1].Z > LakeshoreLayout.WaterlineZ + 3f, "the dock doesn't reach out over the water");
        Assert.True(planks[^1].Z < LakeshoreLayout.DockSouthZ);
        for (var i = 1; i < planks.Count; i++)
        {
            Assert.Equal(LakeshoreLayout.PlankPitch, planks[i].Z - planks[i - 1].Z, 3);
        }
    }

    [Fact]
    public void Shore_rocks_are_deterministic_on_the_shore_and_clear_of_the_dock_and_the_road()
    {
        var rocks = LakeshoreLayout.Rocks();

        Assert.Equal(rocks, LakeshoreLayout.Rocks());
        Assert.NotEmpty(rocks);
        foreach (var rock in rocks)
        {
            Assert.InRange(rock.Z, LakeshoreLayout.ShoreNorthZ, LakeshoreLayout.WaterlineZ);
            Assert.InRange(rock.X, -LakeshoreLayout.HalfWidth, LakeshoreLayout.RiverWestX);
            Assert.True(LakeshoreLayout.IsClearForRock(rock.X, rock.Z));
            Assert.False(rock.X > LakeshoreLayout.DockWestX - 1f && rock.X < LakeshoreLayout.DockEastX + 1f, "a rock is on the dock");
            Assert.True(MathF.Abs(rock.X) >= 1.5f, "a rock is on the road from the exit");
        }
    }
}
