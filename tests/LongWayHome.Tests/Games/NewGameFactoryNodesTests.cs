using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #151: a new Game places one ResourceNode, with no swings taken, at every <c>atStart</c> spawn point and nowhere
/// else (BR-N7).
/// </summary>
public class NewGameFactoryNodesTests
{
    private static readonly NodeSpawn[] Spawns =
    [
        NodeGames.Spawn(2, 3, NodeKind.Tree),
        NodeGames.Spawn(60, 30, NodeKind.Stump),
        NodeGames.Spawn(5, 7, NodeKind.Rock),
        NodeGames.Spawn(10, 20, NodeKind.Tree, atStart: false),
        NodeGames.Spawn(12, 22, NodeKind.Rock, atStart: false),
    ];

    [Fact]
    public void AC1_a_new_game_has_a_node_at_every_atStart_spawn_point_and_none_elsewhere()
    {
        var data = TestGames.Shipped with { NodeSpawns = Spawns };

        var game = NewGameFactory.Create(data, TestGames.Now);

        Assert.Equal(
            ["farm-2-3:Tree:Farm:2:3:0", "farm-5-7:Rock:Farm:5:7:0", "farm-60-30:Stump:Farm:60:30:0"],
            NodeGames.Describe(game.ResourceNodes));
    }

    [Fact]
    public void AC1_a_spawn_point_that_is_not_atStart_has_no_node_in_a_new_game()
    {
        var data = TestGames.Shipped with { NodeSpawns = Spawns };

        var game = NewGameFactory.Create(data, TestGames.Now);

        Assert.NotNull(game.NodeAt(AreaId.Farm, new GridPosition(2, 3)));
        Assert.Null(game.NodeAt(AreaId.Farm, new GridPosition(10, 20)));
        Assert.Null(game.NodeAt(AreaId.Farm, new GridPosition(12, 22)));
    }

    [Fact]
    public void AC1_a_new_game_from_the_shipped_data_has_a_node_at_every_shipped_atStart_spawn_point()
    {
        var atStart = TestGames.Shipped.NodeSpawns.Where(spawn => spawn.AtStart).ToList();
        Assert.True(atStart.Count >= 600, $"the shipped nodes.json has {atStart.Count} atStart spawn points; expected about 750");

        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        Assert.Equal(NodeGames.DescribeSpawns(atStart), NodeGames.Describe(game.ResourceNodes));
    }

    [Fact]
    public void BR_N7_a_new_games_cleared_6_x_6_by_the_farmhouse_has_no_node_while_the_meadow_has_many()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);
        Assert.True(game.ResourceNodes.Count >= 600, $"a new Game has {game.ResourceNodes.Count} nodes; expected about 750");

        var inCleared =
            from x in Enumerable.Range(33, 6)
            from y in Enumerable.Range(0, 6)
            let node = game.NodeAt(AreaId.Farm, new GridPosition(x, y))
            where node is not null
            select node.SpawnId;

        Assert.Empty(inCleared);
    }

    [Fact]
    public void BR_N7_every_node_in_a_new_game_has_no_swings_taken_and_its_spawn_points_kind_and_tile()
    {
        var data = TestGames.Shipped with { NodeSpawns = Spawns };

        var game = NewGameFactory.Create(data, TestGames.Now);

        Assert.Equal(3, game.ResourceNodes.Count);
        Assert.All(game.ResourceNodes, node =>
        {
            var spawn = Spawns.Single(spawn => spawn.Id == node.SpawnId);
            Assert.Equal(spawn.Kind, node.Kind);
            Assert.Equal(spawn.Area, node.Area);
            Assert.Equal(spawn.Position, node.Position);
            Assert.Equal(0, node.SwingsTaken);
        });
    }

    [Fact]
    public void BR_N7_the_opening_can_still_till_plant_and_water_in_the_cleared_6_x_6()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);
        Assert.True(game.ResourceNodes.Count >= 600, $"a new Game has {game.ResourceNodes.Count} nodes; expected about 750");

        var cleared =
            from x in Enumerable.Range(33, 6)
            from y in Enumerable.Range(0, 6)
            select new GridPosition(x, y);

        Assert.All(cleared, position => Assert.Null(game.TillRejection(position, TestGames.Shipped.Tuning.ToolEnergyCost)));
    }
}
