using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #153: clearing a tree or rock records a <see cref="NodeRegrowth"/> dated today; stumps and debris record nothing
/// (BR-N5). In the Night's regrowth step (<see cref="GameState.RegrowNodes"/>, before the date step) a NodeRegrowth at
/// least 2 Nights old grows its node back with 0 swings, unless its Tile is tilled or a tilled Tile is within 3 tiles
/// (Chebyshev); a blocked spawn point keeps waiting. Off the farm only the wait applies (BR-N4).
/// </summary>
public class NodeRegrowthTests
{
    private const int Nights = 2;

    private static readonly GridPosition Spot = new(10, 20);

    private static readonly GridPosition Feet = new(10, 21);

    private static GameTuning Tuning => TestGames.Shipped.Tuning;

    private static GameDate Spring(int day, int year = 1) => new(year, Season.Spring, day);

    /// <summary>A spawn point at <see cref="Spot"/> (or the given position) in the given area.</summary>
    private static NodeSpawn SpawnAt(NodeKind kind, AreaId area = AreaId.Farm, GridPosition? position = null)
    {
        var at = position ?? Spot;
        return new NodeSpawn(
            $"{area}-{at.X}-{at.Y}", area, null, at, kind, AtStart: true, Respawns: kind is not (NodeKind.Stump or NodeKind.Debris));
    }

    /// <summary>The shipped data with only the given spawn points.</summary>
    private static GameData DataWith(params NodeSpawn[] spawns) => TestGames.Shipped with { NodeSpawns = spawns };

    /// <summary>
    /// A Game on <paramref name="date"/> at 150 energy, grass except for <paramref name="tiles"/>, holding the given
    /// nodes and NodeRegrowths (passed through the Game constructor).
    /// </summary>
    private static GameState GameOn(
        GameDate date, ResourceNode[] nodes, NodeRegrowth[]? regrowths = null, params Tile[] tiles)
    {
        var plain = TestGames.OnDate(date, TestGames.ToolsOnly(), tiles);
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
            regrowths);
    }

    private static Tile Tilled(int x, int y) => new(new GridPosition(x, y), TileState.Tilled, false, null);

    /// <summary>Swings the right tool at the Groth tier (2 swings, and a stump can be cleared) until the node falls.</summary>
    private static void Clear(GameState game, NodeSpawn spawn)
    {
        var tool = spawn.Kind is NodeKind.Tree or NodeKind.Stump ? ToolId.Axe : ToolId.Pickaxe;
        for (var swing = 0; swing < 10; swing++)
        {
            var result = game.Swing(
                spawn.Area, spawn.Position, tool, ToolTier.Groth, Tuning.ToolEnergyCost, Tuning.SwingsToClear,
                Tuning.NodeYields, Feet);
            Assert.True(result.Result.IsOk, $"the swing at {spawn.Kind} was refused: {result.Result}");
            if (result.Cleared)
            {
                return;
            }
        }

        Assert.Fail($"the {spawn.Kind} never cleared");
    }

    private static void Night(GameState game, GameData data) => NightProcessor.Run(game, data, DayEndReason.Slept);

    /// <summary>
    /// Clears the node at the spawn point on <paramref name="clearedOn"/>, then checks it is absent the next morning
    /// and standing, with 0 swings, the morning after (BR-N4's 2-Night wait).
    /// </summary>
    private static void AssertGrowsBackOnTheSecondMorning(NodeSpawn spawn, GameDate clearedOn, GameDate firstMorning, GameDate secondMorning)
    {
        var data = DataWith(spawn);
        var game = GameOn(clearedOn, [ResourceNode.FromSpawn(spawn)]);
        Clear(game, spawn);
        Assert.Null(game.NodeAt(spawn.Area, spawn.Position));

        Night(game, data);

        Assert.Equal(firstMorning, game.Date);
        Assert.Null(game.NodeAt(spawn.Area, spawn.Position));

        Night(game, data);

        Assert.Equal(secondMorning, game.Date);
        var regrown = game.NodeAt(spawn.Area, spawn.Position);
        Assert.NotNull(regrown);
        Assert.Equal(
            $"{spawn.Id}:{spawn.Kind}:{spawn.Area}:{spawn.Position.X}:{spawn.Position.Y}:0",
            Assert.Single(NodeGames.Describe(game.ResourceNodes)));
        Assert.Empty(game.NodeRegrowths);
    }

    // ---- AC1, AC2: the 2-Night wait through the Night ----

    [Fact]
    public void AC1_a_tree_cleared_on_spring_3_is_absent_on_spring_4_and_stands_with_0_swings_on_spring_5() =>
        AssertGrowsBackOnTheSecondMorning(SpawnAt(NodeKind.Tree), Spring(3), Spring(4), Spring(5));

    [Fact]
    public void AC2_a_rock_cleared_on_spring_3_is_absent_on_spring_4_and_stands_with_0_swings_on_spring_5() =>
        AssertGrowsBackOnTheSecondMorning(SpawnAt(NodeKind.Rock), Spring(3), Spring(4), Spring(5));

    // ---- AC3, BR-N5: stumps and debris never grow back ----

    [Fact]
    public void AC3_a_cleared_stump_never_grows_back_while_a_tree_cleared_the_same_day_does()
    {
        var stump = SpawnAt(NodeKind.Stump);
        var tree = SpawnAt(NodeKind.Tree, position: new GridPosition(40, 20));
        var data = DataWith(stump, tree);
        var game = GameOn(Spring(3), [ResourceNode.FromSpawn(stump), ResourceNode.FromSpawn(tree)]);
        Clear(game, stump);
        Clear(game, tree);

        for (var night = 0; night < 5; night++)
        {
            Night(game, data);
        }

        Assert.NotNull(game.NodeAt(AreaId.Farm, tree.Position));
        Assert.Null(game.NodeAt(AreaId.Farm, stump.Position));
    }

    [Fact]
    public void AC3_clearing_a_stump_records_no_node_regrowth()
    {
        var stump = SpawnAt(NodeKind.Stump);
        var tree = SpawnAt(NodeKind.Tree, position: new GridPosition(40, 20));
        var game = GameOn(Spring(3), [ResourceNode.FromSpawn(stump), ResourceNode.FromSpawn(tree)]);

        Clear(game, stump);
        Clear(game, tree);

        // The tree's record is the control: the Game records regrowths, just not for the stump.
        Assert.Equal([new NodeRegrowth(tree.Id, Spring(3))], game.NodeRegrowths);
    }

    [Theory]
    [InlineData(NodeKind.Stump)]
    [InlineData(NodeKind.Debris)]
    public void BR_N5_clearing_a_stump_or_debris_records_nothing_while_a_tree_records_its_regrowth(NodeKind kind)
    {
        var never = SpawnAt(kind);
        var tree = SpawnAt(NodeKind.Tree, position: new GridPosition(40, 20));
        var game = GameOn(Spring(3), [ResourceNode.FromSpawn(never), ResourceNode.FromSpawn(tree)]);

        Clear(game, never);
        Clear(game, tree);

        Assert.Equal([tree.Id], game.NodeRegrowths.Select(regrowth => regrowth.SpawnId));
    }

    [Fact]
    public void BR_N5_a_node_built_without_a_spawn_point_respawns_unless_it_is_a_stump_or_debris()
    {
        var respawns = Enum.GetValues<NodeKind>()
            .ToDictionary(kind => kind, kind => new ResourceNode("x", kind, AreaId.Farm, Spot).Respawns);

        Assert.Equal(
            new Dictionary<NodeKind, bool>
            {
                [NodeKind.Tree] = true,
                [NodeKind.Stump] = false,
                [NodeKind.Rock] = true,
                [NodeKind.RoskDeposit] = true,
                [NodeKind.GrothDeposit] = true,
                [NodeKind.Glimmercap] = true,
                [NodeKind.Debris] = false,
            },
            respawns);
    }

    // ---- BR-N4: recording a regrowth ----

    [Theory]
    [InlineData(NodeKind.Tree)]
    [InlineData(NodeKind.Rock)]
    public void BR_N4_clearing_a_tree_or_rock_records_one_node_regrowth_dated_today(NodeKind kind)
    {
        var spawn = SpawnAt(kind);
        var game = GameOn(new GameDate(2, Season.Fall, 17), [ResourceNode.FromSpawn(spawn)]);

        Clear(game, spawn);

        Assert.Equal([new NodeRegrowth(spawn.Id, new GameDate(2, Season.Fall, 17))], game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_swing_that_does_not_clear_the_node_records_nothing_until_the_clearing_swing()
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(3), [ResourceNode.FromSpawn(spawn)]);

        // Basic tier: 4 swings to clear (BR-N2).
        var counts = new List<int>();
        for (var swing = 0; swing < 4; swing++)
        {
            game.Swing(
                AreaId.Farm, Spot, ToolId.Axe, ToolTier.Basic, Tuning.ToolEnergyCost, Tuning.SwingsToClear,
                Tuning.NodeYields, Feet);
            counts.Add(game.NodeRegrowths.Count);
        }

        Assert.Equal([0, 0, 0, 1], counts);
    }

    [Fact]
    public void BR_N4_a_game_refuses_two_node_regrowths_for_one_spawn_point()
    {
        Assert.Throws<ArgumentException>(() => GameOn(
            Spring(5),
            [],
            [new NodeRegrowth("farm-10-20", Spring(3)), new NodeRegrowth("farm-10-20", Spring(4))]));
    }

    // ---- BR-N4: the Night's regrowth step, on the Game ----

    [Fact]
    public void BR_N4_a_free_spawn_point_two_nights_after_clearing_grows_a_fresh_node_and_its_regrowth_goes()
    {
        // Regrowth runs before the date step: on Spring 4 the Night ending it is the second since the Spring 3 clearing.
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))]);

        game.RegrowNodes([spawn], Nights);

        Assert.Equal(["Farm-10-20:Tree:Farm:10:20:0"], NodeGames.Describe(game.ResourceNodes));
        Assert.True(game.NodeAt(AreaId.Farm, Spot)!.Respawns);
        Assert.Empty(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_regrowth_in_its_first_night_keeps_waiting()
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(3), [], [new NodeRegrowth(spawn.Id, Spring(3))]);

        game.RegrowNodes([spawn], Nights);

        Assert.Empty(game.ResourceNodes);
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(3))], game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_regrowth_long_past_its_wait_still_grows_when_its_spot_is_free()
    {
        var spawn = SpawnAt(NodeKind.Rock);
        var game = GameOn(new GameDate(1, Season.Fall, 2), [], [new NodeRegrowth(spawn.Id, Spring(1))]);

        game.RegrowNodes([spawn], Nights);

        Assert.NotNull(game.NodeAt(AreaId.Farm, Spot));
        Assert.Empty(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_the_wait_follows_the_tuning_nights()
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))]);

        game.RegrowNodes([spawn], nights: 3);

        Assert.Empty(game.ResourceNodes);
        Assert.Single(game.NodeRegrowths);

        game.AdvanceDate();
        game.RegrowNodes([spawn], nights: 3);

        Assert.NotNull(game.NodeAt(AreaId.Farm, Spot));
    }

    [Fact]
    public void BR_N4_a_blocked_spawn_point_keeps_waiting_night_after_night()
    {
        // BR-T8: a tilled Tile never reverts, so the spawn point next to it waits for good, its record kept.
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))], Tilled(Spot.X + 1, Spot.Y));

        for (var night = 0; night < 5; night++)
        {
            game.RegrowNodes([spawn], Nights);
            game.AdvanceDate();
        }

        Assert.Empty(game.ResourceNodes);
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(3))], game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_an_off_farm_spawn_point_regrows_by_the_wait_alone()
    {
        // Off the farm there are no Tiles: the farm Tile with the same coordinates being tilled doesn't block it.
        var spawn = SpawnAt(NodeKind.Tree, AreaId.ForestPath);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))], Tilled(Spot.X, Spot.Y));

        game.RegrowNodes([spawn], Nights);

        Assert.NotNull(game.NodeAt(AreaId.ForestPath, Spot));
        Assert.Null(game.NodeAt(AreaId.Farm, Spot));
        Assert.Empty(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_an_off_farm_regrowth_in_its_first_night_keeps_waiting()
    {
        var spawn = SpawnAt(NodeKind.Rock, AreaId.ForestPath);
        var game = GameOn(Spring(3), [], [new NodeRegrowth(spawn.Id, Spring(3))]);

        game.RegrowNodes([spawn], Nights);

        Assert.Empty(game.ResourceNodes);
        Assert.Single(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_only_the_due_regrowths_grow_and_the_others_are_kept()
    {
        var due = SpawnAt(NodeKind.Tree);
        var early = SpawnAt(NodeKind.Rock, position: new GridPosition(40, 20));
        var game = GameOn(
            Spring(4), [], [new NodeRegrowth(due.Id, Spring(3)), new NodeRegrowth(early.Id, Spring(4))]);

        game.RegrowNodes([due, early], Nights);

        Assert.Equal(["Farm-10-20:Tree:Farm:10:20:0"], NodeGames.Describe(game.ResourceNodes));
        Assert.Equal([new NodeRegrowth(early.Id, Spring(4))], game.NodeRegrowths);
    }

    // ---- AC4, AC5: tilled soil blocks ----

    [Fact]
    public void AC4_a_spawn_point_whose_tile_was_tilled_after_clearing_does_not_grow_back_and_keeps_its_regrowth()
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(3), [ResourceNode.FromSpawn(spawn)]);
        Clear(game, spawn);
        Assert.True(game.Till(Spot, Tuning.ToolEnergyCost).IsOk);
        var data = DataWith(spawn);

        Night(game, data);
        Night(game, data);
        Night(game, data);

        Assert.Null(game.NodeAt(AreaId.Farm, Spot));
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(3))], game.NodeRegrowths);
    }

    [Fact]
    public void AC4_regrow_nodes_leaves_a_spawn_point_on_a_tilled_tile_waiting()
    {
        var spawn = SpawnAt(NodeKind.Rock);
        var game = GameOn(Spring(10), [], [new NodeRegrowth(spawn.Id, Spring(3))], Tilled(Spot.X, Spot.Y));

        game.RegrowNodes([spawn], Nights);

        Assert.Empty(game.ResourceNodes);
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(3))], game.NodeRegrowths);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(3, 0)]
    [InlineData(0, -3)]
    [InlineData(-3, 3)]
    [InlineData(-3, -3)]
    [InlineData(1, 2)]
    [InlineData(-1, 0)]
    public void AC5_a_tilled_tile_within_3_tiles_blocks_regrowth(int dx, int dy)
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))], Tilled(Spot.X + dx, Spot.Y + dy));

        game.RegrowNodes([spawn], Nights);

        Assert.Null(game.NodeAt(AreaId.Farm, Spot));
        Assert.Equal([new NodeRegrowth(spawn.Id, Spring(3))], game.NodeRegrowths);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(4, 4)]
    [InlineData(0, -4)]
    [InlineData(-4, 3)]
    [InlineData(2, 4)]
    public void AC5_with_the_nearest_tilled_tile_4_tiles_away_the_node_grows_back(int dx, int dy)
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))], Tilled(Spot.X + dx, Spot.Y + dy));

        game.RegrowNodes([spawn], Nights);

        Assert.NotNull(game.NodeAt(AreaId.Farm, Spot));
        Assert.Empty(game.NodeRegrowths);
    }

    [Fact]
    public void AC5_a_spawn_point_at_the_plot_corner_checks_only_tiles_on_the_plot()
    {
        // The 7 x 7 square is clipped to the grid: a corner spawn point with no tilled Tile near it grows.
        var corner = new GridPosition(0, 0);
        var spawn = SpawnAt(NodeKind.Rock, position: corner);
        var game = GameOn(Spring(4), [], [new NodeRegrowth(spawn.Id, Spring(3))], Tilled(4, 4));

        game.RegrowNodes([spawn], Nights);

        Assert.NotNull(game.NodeAt(AreaId.Farm, corner));
    }

    // ---- AC6: across Seasons and Years ----

    [Fact]
    public void AC6_a_tree_cleared_on_spring_28_of_year_1_stands_again_on_summer_2() =>
        AssertGrowsBackOnTheSecondMorning(
            SpawnAt(NodeKind.Tree), Spring(28), new GameDate(1, Season.Summer, 1), new GameDate(1, Season.Summer, 2));

    [Fact]
    public void AC6_a_tree_cleared_on_winter_28_of_year_1_stands_again_on_spring_2_of_year_2() =>
        AssertGrowsBackOnTheSecondMorning(
            SpawnAt(NodeKind.Tree), new GameDate(1, Season.Winter, 28), Spring(1, year: 2), Spring(2, year: 2));

    // ---- AC9: on the farm, through the session ----

    [Fact]
    public void AC9_a_regrown_farm_tree_is_in_the_node_list_and_blocks_tilling_the_morning_it_grows_back()
    {
        var spawn = SpawnAt(NodeKind.Tree);
        var plain = TestGames.WithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic)),
            date: Spring(3));
        var session = new GameSession(NodeGames.WithNodes(plain, ResourceNode.FromSpawn(spawn)), DataWith(spawn));
        while (session.Game.NodeAt(AreaId.Farm, Spot) is not null)
        {
            Assert.True(session.Swing(Spot, ToolId.Axe).IsOk);
        }

        Assert.True(session.Sleep().IsOk);
        var nodesChanged = 0;
        session.NodesChanged += () => nodesChanged++;

        Assert.True(session.Sleep().IsOk);

        Assert.Equal(Spring(5), session.Game.Date);
        Assert.NotNull(session.Game.NodeAt(AreaId.Farm, Spot));
        Assert.Contains(session.Game.ResourceNodes, node => node.SpawnId == spawn.Id);
        Assert.True(nodesChanged >= 1, "NodesChanged was not raised on the Night the tree grew back");
        Assert.Equal(ActionResult.Rejected(Rejection.NodeInTheWay), session.Till(Spot));
    }
}
