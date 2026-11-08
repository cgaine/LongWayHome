using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #154 on the shipped data: a new Game has a <c>tree</c> node on the forest path at the Tile under each
/// <see cref="ForestPathLayout"/> trunk and 8 <c>rock</c> nodes on bank Tiles (tile centre x 2.5–4.0) (AC1, BR-N7);
/// no forest path spawn point is on a path Tile or in front of either exit (AC5). The path and exit Tiles are worked
/// out here from the spec, not from any production helper.
/// </summary>
public class ForestPathNodesTests
{
    private const int Width = 14;

    private const int Height = 40;

    /// <summary>The forest path's spawn points in the shipped <c>nodes.json</c>.</summary>
    private static List<NodeSpawn> ShippedForestSpawns() =>
        TestGames.Shipped.NodeSpawns.Where(spawn => spawn.Area == AreaId.ForestPath).ToList();

    /// <summary>The forest path's nodes in a new Game made from the shipped data.</summary>
    private static List<ResourceNode> NewGameForestNodes() =>
        NewGameFactory.Create(TestGames.Shipped, TestGames.Now).ResourceNodes
            .Where(node => node.Area == AreaId.ForestPath)
            .ToList();

    /// <summary>The Tile (X, Y) containing world (x, z): tile (X, Y) covers x X − 7..X − 6 and z Y − 20..Y − 19.</summary>
    private static (int X, int Y) TileContaining(float x, float z) =>
        ((int)MathF.Floor(x + 7f), (int)MathF.Floor(z + 20f));

    /// <summary>
    /// Whether the Tile's 1 m square comes within the path's half-width (1 m) of the path's centre line at any z across
    /// it: its x range [cx − 0.5, cx + 0.5] overlaps [PathCentreX(z) − 1, PathCentreX(z) + 1] for some z in its z range.
    /// </summary>
    private static bool IsPathTile(int x, int y)
    {
        var centreX = x - 7f + 0.5f;
        const int Samples = 200;
        for (var i = 0; i <= Samples; i++)
        {
            var z = y - 20f + ((float)i / Samples);
            if (MathF.Abs(centreX - ForestPathLayout.PathCentreX(z)) <= 0.5f + ForestPathLayout.PathHalfWidth)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The Tiles in front of either exit: X 4–9 with Y 0–3 (the farm's, north) or Y 36–39 (the town's, south).</summary>
    private static bool IsExitApproachTile(int x, int y) =>
        x is >= 4 and <= 9 && (y is >= 0 and <= 3 || y is >= 36 and <= 39);

    // ---- AC1 / BR-N7 ----

    [Fact]
    public void AC1_a_new_game_has_a_forest_path_tree_on_the_tile_under_every_layout_trunk()
    {
        var expected = ForestPathLayout.Trees()
            .Select(tree => TileContaining(tree.X, tree.Z))
            .Order()
            .ToList();

        var trees = NewGameForestNodes()
            .Where(node => node.Kind == NodeKind.Tree)
            .Select(node => (node.Position.X, node.Position.Y))
            .Order()
            .ToList();

        Assert.Equal(34, expected.Count);
        Assert.Equal(expected, trees);
    }

    [Fact]
    public void AC1_a_new_game_has_8_forest_path_rocks_on_bank_tiles()
    {
        var rocks = NewGameForestNodes().Where(node => node.Kind == NodeKind.Rock).ToList();

        Assert.Equal(8, rocks.Count);
        Assert.All(rocks, rock =>
        {
            var centreX = rock.Position.X - 7f + 0.5f;
            Assert.InRange(centreX, 2.5f, 4.0f);
        });
    }

    [Fact]
    public void AC1_the_forest_path_rocks_are_spread_along_the_strip()
    {
        var rocks = NewGameForestNodes().Where(node => node.Kind == NodeKind.Rock).ToList();

        Assert.Equal(8, rocks.Select(rock => rock.Position.Y).Distinct().Count());
        Assert.Contains(rocks, rock => rock.Position.Y < Height / 2);
        Assert.Contains(rocks, rock => rock.Position.Y >= Height / 2);
    }

    [Fact]
    public void AC1_the_forest_path_has_only_trees_and_rocks_all_standing_in_a_new_game()
    {
        var spawns = ShippedForestSpawns();

        Assert.Equal(42, spawns.Count);
        Assert.All(spawns, spawn => Assert.Contains(spawn.Kind, new[] { NodeKind.Tree, NodeKind.Rock }));
        Assert.Equal(
            spawns.Select(spawn => spawn.Id).Order(StringComparer.Ordinal),
            NewGameForestNodes().Select(node => node.SpawnId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AC1_forest_path_spawn_points_are_atStart_respawning_and_named_by_their_tile()
    {
        var spawns = ShippedForestSpawns();

        Assert.NotEmpty(spawns);
        Assert.All(spawns, spawn =>
        {
            Assert.Equal($"forest_path-{spawn.Position.X}-{spawn.Position.Y}", spawn.Id);
            Assert.True(spawn.AtStart, $"{spawn.Id} is not atStart");
            Assert.True(spawn.Respawns, $"{spawn.Id} does not respawn");
            Assert.Null(spawn.MineLevel);
            Assert.InRange(spawn.Position.X, 0, Width - 1);
            Assert.InRange(spawn.Position.Y, 0, Height - 1);
        });
    }

    [Fact]
    public void AC1_layout_TreeTiles_gives_the_tile_containing_each_trunk_in_tree_order()
    {
        var expected = ForestPathLayout.Trees().Select(tree => TileContaining(tree.X, tree.Z)).ToList();

        var tiles = ForestPathLayout.TreeTiles().Select(tile => (tile.X, tile.Y)).ToList();

        Assert.Equal(expected, tiles);
    }

    // ---- AC5 ----

    [Fact]
    public void AC5_no_forest_path_spawn_point_is_on_a_path_tile()
    {
        var spawns = ShippedForestSpawns();

        Assert.NotEmpty(spawns);
        Assert.All(spawns, spawn =>
            Assert.False(IsPathTile(spawn.Position.X, spawn.Position.Y), $"{spawn.Id} is on a path Tile"));
    }

    [Fact]
    public void AC5_no_forest_path_spawn_point_is_in_front_of_either_exit()
    {
        var spawns = ShippedForestSpawns();

        Assert.NotEmpty(spawns);
        Assert.All(spawns, spawn =>
            Assert.False(IsExitApproachTile(spawn.Position.X, spawn.Position.Y), $"{spawn.Id} is in front of an exit"));
    }

    [Fact]
    public void Path_tile_definition_reaches_both_exits_and_every_row()
    {
        // Sanity check on this test's own definition: the exits' Tiles (x = 0 at z ±20 is tile X 7) are path Tiles, and
        // every row of the strip has at least one path Tile.
        Assert.True(IsPathTile(7, 0));
        Assert.True(IsPathTile(7, Height - 1));
        for (var y = 0; y < Height; y++)
        {
            var row = y;
            Assert.Contains(Enumerable.Range(0, Width), x => IsPathTile(x, row));
        }
    }
}
