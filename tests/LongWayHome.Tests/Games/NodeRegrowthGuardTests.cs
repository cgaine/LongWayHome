using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #153 paths the acceptance tests don't drive: <see cref="GameState.RegrowNodes"/>'s argument guards, a regrowth whose
/// spawn point is no longer in the spawn list (dropped), a Tile already holding another node (keeps waiting), and the
/// Game constructor's checks on its NodeRegrowths.
/// </summary>
public class NodeRegrowthGuardTests
{
    private static readonly GridPosition Spot = new(10, 20);

    private static GameDate Spring(int day) => new(1, Season.Spring, day);

    private static NodeSpawn Spawn(string id, AreaId area = AreaId.Farm, NodeKind kind = NodeKind.Tree) =>
        new(id, area, null, Spot, kind, AtStart: true, Respawns: true);

    private static GameState GameOn(GameDate date, ResourceNode[] nodes, NodeRegrowth?[] regrowths)
    {
        var plain = TestGames.OnDate(date, TestGames.ToolsOnly());
        return new GameState(
            plain.Id,
            plain.Date,
            plain.ClockMinutes,
            plain.Weather,
            plain.LastDayEnd,
            plain.CreatedAt,
            plain.SavedAt,
            plain.Player,
            plain.FarmPlot,
            plain.ShippingBin,
            plain.Residents,
            plain.OpeningDone,
            plain.PurchasedListings,
            nodes,
            regrowths!);
    }

    [Fact]
    public void BR_N4_regrow_nodes_refuses_a_null_spawn_list()
    {
        var game = GameOn(Spring(4), [], [new NodeRegrowth("a", Spring(3))]);

        Assert.Throws<ArgumentNullException>(() => game.RegrowNodes(null!, 2));
        Assert.Single(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_regrow_nodes_refuses_a_null_spawn_and_changes_nothing()
    {
        var game = GameOn(Spring(4), [], [new NodeRegrowth("a", Spring(3))]);

        Assert.Throws<ArgumentNullException>(() => game.RegrowNodes([Spawn("a"), null!], 2));
        Assert.Empty(game.ResourceNodes);
        Assert.Single(game.NodeRegrowths);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BR_N4_regrow_nodes_refuses_a_wait_below_1_night_and_changes_nothing(int nights)
    {
        var game = GameOn(Spring(10), [], [new NodeRegrowth("a", Spring(3))]);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.RegrowNodes([Spawn("a")], nights));
        Assert.Empty(game.ResourceNodes);
        Assert.Single(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_regrowth_whose_spawn_point_is_not_in_the_spawn_list_is_dropped()
    {
        // It can never grow back, so it isn't kept forever; the one still in the list grows as usual.
        var kept = Spawn("kept", AreaId.ForestPath);
        var game = GameOn(Spring(4), [], [new NodeRegrowth("gone", Spring(4)), new NodeRegrowth(kept.Id, Spring(3))]);

        game.RegrowNodes([kept], 2);

        Assert.Empty(game.NodeRegrowths);
        Assert.Equal(["kept:Tree:ForestPath:10:20:0"], NodeGames.Describe(game.ResourceNodes));
    }

    [Theory]
    [InlineData(AreaId.Farm)]
    [InlineData(AreaId.ForestPath)]
    public void BR_N4_a_spawn_point_whose_tile_holds_another_node_keeps_waiting(AreaId area)
    {
        var other = new ResourceNode("other", NodeKind.Rock, area, Spot);
        var spawn = Spawn("a", area);
        var game = GameOn(Spring(4), [other], [new NodeRegrowth(spawn.Id, Spring(3))]);

        game.RegrowNodes([spawn], 2);

        Assert.Same(other, game.NodeAt(area, Spot));
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(3))], game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_regrown_node_can_be_found_on_its_tile_and_cleared_again()
    {
        // The Tile index is kept in sync, so the regrown node blocks and takes swings like any other.
        var spawn = Spawn("a", AreaId.ForestPath);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))]);
        game.RegrowNodes([spawn], 2);
        var tuning = TestGames.Shipped.Tuning;

        var swings = 0;
        while (game.NodeAt(AreaId.ForestPath, Spot) is not null && swings++ < 10)
        {
            Assert.True(game.Swing(
                AreaId.ForestPath, Spot, ToolId.Axe, ToolTier.Groth, tuning.ToolEnergyCost, tuning.SwingsToClear,
                tuning.NodeYields, new GridPosition(10, 21)).Result.IsOk);
        }

        Assert.Empty(game.ResourceNodes);
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(4))], game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_game_refuses_a_null_node_regrowth() =>
        Assert.Throws<ArgumentException>(() => GameOn(Spring(4), [], [null]));

    [Fact]
    public void BR_N4_a_game_refuses_a_node_regrowth_with_a_null_spawn_id() =>
        Assert.Throws<ArgumentException>(() => GameOn(Spring(4), [], [new NodeRegrowth(null!, Spring(3))]));

    [Fact]
    public void BR_N4_a_game_refuses_a_node_regrowth_for_a_spawn_point_whose_node_stands()
    {
        // Otherwise clearing that node would record a second NodeRegrowth for one spawn point.
        Assert.Throws<ArgumentException>(() => GameOn(
            Spring(4), [NodeGames.Node(10, 20)], [new NodeRegrowth("farm-10-20", Spring(3))]));
    }
}
