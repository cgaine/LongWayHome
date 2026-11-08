using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #151: the shipped <c>game/data/nodes.json</c>, linked into the test output by the test project. The farm is
/// overgrown with about 750 trees, stumps and rocks, except in the cleared 6 x 6 by the farmhouse (X 33–38, Y 0–5)
/// and on the path to the south exit (X 34–37, Y 6–35).
/// </summary>
public class ShippedNodesTests
{
    /// <summary>The issue asks for about 750 farm spawn points (one per 3 grid Tiles outside the clear areas).</summary>
    private const int MinFarmSpawns = 600;
    private const int MaxFarmSpawns = 900;

    private static readonly GameData Data = LoadShippedData();

    private static IReadOnlyList<NodeSpawn> FarmSpawns() => Data.NodeSpawns.Where(spawn => spawn.Area == AreaId.Farm).ToList();

    /// <summary>The farm's spawn points, asserted to be about the issue's density so no check below passes on none.</summary>
    private static IReadOnlyList<NodeSpawn> OvergrownFarm()
    {
        var spawns = FarmSpawns();
        Assert.True(
            spawns.Count is >= MinFarmSpawns and <= MaxFarmSpawns,
            $"nodes.json has {spawns.Count} farm spawn points; expected between {MinFarmSpawns} and {MaxFarmSpawns} (about 750)");
        return spawns;
    }

    [Fact]
    public void AC2_no_farm_spawn_point_lies_in_the_cleared_6_x_6_by_the_farmhouse()
    {
        var inside = OvergrownFarm()
            .Where(spawn => spawn.Position.X is >= 33 and <= 38 && spawn.Position.Y is >= 0 and <= 5)
            .Select(spawn => spawn.Id)
            .ToList();

        Assert.True(inside.Count == 0, $"spawn points in the cleared 6 x 6 (X 33–38, Y 0–5): {string.Join(", ", inside)}");
    }

    [Fact]
    public void AC3_no_farm_spawn_point_lies_on_the_path_from_the_south_exit_to_the_farmhouse()
    {
        var onPath = OvergrownFarm()
            .Where(spawn => spawn.Position.X is >= 34 and <= 37 && spawn.Position.Y is >= 6 and <= 35)
            .Select(spawn => spawn.Id)
            .ToList();

        Assert.True(onPath.Count == 0, $"spawn points on the kept-clear path (X 34–37, Y 6–35): {string.Join(", ", onPath)}");
    }

    [Fact]
    public void AC3_no_two_tree_spawn_points_are_on_touching_tiles()
    {
        var trees = OvergrownFarm().Where(spawn => spawn.Kind == NodeKind.Tree).ToList();
        Assert.NotEmpty(trees);
        var occupied = trees.Select(tree => (tree.Position.X, tree.Position.Y)).ToHashSet();

        var touching = trees
            .Where(tree =>
                Neighbours(tree.Position.X, tree.Position.Y).Any(occupied.Contains))
            .Select(tree => tree.Id)
            .ToList();

        Assert.True(touching.Count == 0, $"trees with another tree on a touching Tile: {string.Join(", ", touching.Take(20))}");
    }

    [Fact]
    public void AC4_the_farm_has_tree_stump_and_rock_spawn_points()
    {
        var kinds = OvergrownFarm().Select(spawn => spawn.Kind).ToHashSet();

        Assert.Equal(new HashSet<NodeKind> { NodeKind.Tree, NodeKind.Stump, NodeKind.Rock }, kinds);
    }

    [Fact]
    public void AC4_the_farm_is_roughly_60_percent_trees_15_percent_stumps_and_25_percent_rocks()
    {
        var spawns = OvergrownFarm();
        double Share(NodeKind kind) => spawns.Count(spawn => spawn.Kind == kind) / (double)spawns.Count;

        Assert.InRange(Share(NodeKind.Tree), 0.50, 0.70);
        Assert.InRange(Share(NodeKind.Stump), 0.10, 0.20);
        Assert.InRange(Share(NodeKind.Rock), 0.18, 0.32);
    }

    [Fact]
    public void AC4_every_tree_and_rock_respawns()
    {
        var notRespawning = OvergrownFarm()
            .Where(spawn => spawn.Kind is NodeKind.Tree or NodeKind.Rock && !spawn.Respawns)
            .Select(spawn => spawn.Id)
            .ToList();

        Assert.True(notRespawning.Count == 0, $"trees or rocks with respawns false: {string.Join(", ", notRespawning.Take(20))}");
    }

    [Fact]
    public void BR_N5_every_stump_has_respawns_false()
    {
        var stumps = OvergrownFarm().Where(spawn => spawn.Kind == NodeKind.Stump).ToList();
        Assert.NotEmpty(stumps);

        Assert.All(stumps, stump => Assert.False(stump.Respawns, $"{stump.Id} is a stump that respawns (BR-N5)"));
    }

    [Fact]
    public void BR_N7_every_farm_spawn_point_is_at_start()
    {
        var spawns = OvergrownFarm();

        Assert.All(spawns, spawn => Assert.True(spawn.AtStart, $"{spawn.Id} is not atStart"));
    }

    [Fact]
    public void Farm_spawn_ids_are_farm_x_y_matching_their_position()
    {
        var spawns = OvergrownFarm();

        Assert.All(spawns, spawn => Assert.Equal($"farm-{spawn.Position.X}-{spawn.Position.Y}", spawn.Id));
        Assert.All(spawns, spawn => Assert.Null(spawn.MineLevel));
    }

    private static IEnumerable<(int X, int Y)> Neighbours(int x, int y) =>
        from dx in new[] { -1, 0, 1 }
        from dy in new[] { -1, 0, 1 }
        where dx != 0 || dy != 0
        select (x + dx, y + dy);

    private static GameData LoadShippedData()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "data");
        string Read(string fileName) => File.ReadAllText(Path.Combine(folder, fileName));

        return new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName),
            Read(JsonGameDataSource.NodesFileName)).Load();
    }
}
