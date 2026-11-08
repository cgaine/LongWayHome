using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #152: <see cref="GameState.SwingRejection"/>, <see cref="GameState.Swing"/> and <see cref="GameState.ResetSwings"/>.
/// The axe chops trees and stumps, the pickaxe breaks rocks (BR-N1); each swing costs 2 energy (BR-P2) and a node falls
/// on its tier's count, Basic 4, Rosk 3, Groth 2 (BR-N2), giving its yield through BR-K3 and freeing its Tile (BR-N3,
/// BR-T2). Only a Groth axe clears a stump (BR-N6). The tuning is built here, not read from the shipped data, so these
/// domain tests don't depend on <c>tuning.json</c>.
/// </summary>
public class SwingTests
{
    private const int Cost = 2;

    private static readonly GridPosition Target = new(10, 20);

    private static readonly GridPosition Feet = new(10, 21);

    /// <summary>The canonical swings to clear a node (canonical-values.md ToolTier): basic 4, rosk 3, groth 2.</summary>
    private static readonly IReadOnlyDictionary<ToolTier, int> SwingsToClear = new Dictionary<ToolTier, int>
    {
        [ToolTier.Basic] = 4,
        [ToolTier.Rosk] = 3,
        [ToolTier.Groth] = 2,
    };

    /// <summary>The issue's node yields: a tree 5 wood + 2 sap, a rock 3 stone, a stump 4 wood.</summary>
    private static readonly IReadOnlyDictionary<NodeKind, IReadOnlyList<MaterialStack>> NodeYields =
        new Dictionary<NodeKind, IReadOnlyList<MaterialStack>>
        {
            [NodeKind.Tree] = [new MaterialStack(MaterialId.Wood, 5), new MaterialStack(MaterialId.Sap, 2)],
            [NodeKind.Rock] = [new MaterialStack(MaterialId.Stone, 3)],
            [NodeKind.Stump] = [new MaterialStack(MaterialId.Wood, 4)],
        };

    private static ItemRef Wood => ItemRef.Material(MaterialId.Wood);

    private static ItemRef Sap => ItemRef.Material(MaterialId.Sap);

    private static ItemRef Stone => ItemRef.Material(MaterialId.Stone);

    /// <summary>A Game whose Player has the given energy, the hoe and can plus the axe and pickaxe at the given tiers.</summary>
    private static GameState GameWith(
        NodeKind kind, int energy = 150, ToolTier axe = ToolTier.Basic, ToolTier pickaxe = ToolTier.Basic, int swingsTaken = 0) =>
        NodeGames.WithNodes(
            TestGames.WithBackpack(
                TestGames.RestoredBackpack(
                    new BackpackSlot(2, ItemRef.Tool(ToolId.Axe), 1, axe),
                    new BackpackSlot(3, ItemRef.Tool(ToolId.Pickaxe), 1, pickaxe)),
                energy: energy),
            NodeGames.Node(Target.X, Target.Y, kind, swingsTaken));

    /// <summary>Swings the tool at <see cref="Target"/> at the tier its Backpack Slot holds.</summary>
    private static SwingResult Swing(GameState game, ToolId tool) =>
        game.Swing(
            AreaId.Farm, Target, tool, game.Player.Backpack.TierOf(tool)!.Value, Cost, SwingsToClear, NodeYields, Feet);

    private static Rejection? RejectionOf(GameState game, ToolId tool) =>
        game.SwingRejection(AreaId.Farm, Target, tool, game.Player.Backpack.TierOf(tool)!.Value, Cost);

    /// <summary>Energy, every Tile, every Slot, the nodes and the DroppedItems, to prove a swing changed nothing.</summary>
    private static (int Energy, string Tiles, string Inventory, string Nodes, int Dropped) State(GameState game)
    {
        var (energy, tiles, inventory) = TestGames.FullSnapshot(game);
        return (energy, tiles, inventory, string.Join(",", NodeGames.Describe(game.ResourceNodes)), game.DroppedItems.Count);
    }

    // ---- AC1 / BR-N2 / BR-P2 / BR-N3: the Basic axe at a tree ----

    [Fact]
    public void AC1_four_basic_axe_swings_at_a_tree_spend_8_energy_count_1_2_3_then_clear_it_for_5_wood_and_2_sap()
    {
        var game = GameWith(NodeKind.Tree);

        for (var swing = 1; swing <= 3; swing++)
        {
            var result = Swing(game, ToolId.Axe);

            Assert.Equal(ActionResult.Ok, result.Result);
            Assert.False(result.Cleared, $"the tree fell on swing {swing}");
            Assert.Equal(swing, game.NodeAt(AreaId.Farm, Target)!.SwingsTaken);
            Assert.Equal(150 - (swing * Cost), game.Player.Energy);
        }

        var fourth = Swing(game, ToolId.Axe);

        Assert.Equal(ActionResult.Ok, fourth.Result);
        Assert.True(fourth.Cleared);
        Assert.Equal(142, game.Player.Energy);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(5, game.Player.Backpack.Quantity(Wood));
        Assert.Equal(2, game.Player.Backpack.Quantity(Sap));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_N2_a_basic_tree_is_still_standing_after_three_swings_and_falls_on_the_fourth()
    {
        var game = GameWith(NodeKind.Tree);

        var results = Enumerable.Range(0, 4).Select(_ => Swing(game, ToolId.Axe).Cleared).ToList();

        Assert.Equal([false, false, false, true], results);
        Assert.Empty(game.ResourceNodes);
    }

    [Fact]
    public void BR_P2_each_swing_costs_exactly_the_tool_energy_cost()
    {
        var game = GameWith(NodeKind.Rock, energy: 7);

        Swing(game, ToolId.Pickaxe);

        Assert.Equal(5, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_a_swing_with_exactly_2_energy_is_allowed_and_leaves_0()
    {
        var game = GameWith(NodeKind.Tree, energy: 2);

        Assert.Null(RejectionOf(game, ToolId.Axe));
        var result = Swing(game, ToolId.Axe);

        Assert.Equal(ActionResult.Ok, result.Result);
        Assert.Equal(0, game.Player.Energy);
        Assert.Equal(1, game.NodeAt(AreaId.Farm, Target)!.SwingsTaken);
    }

    // ---- AC2 / BR-N3: the Basic pickaxe at a rock ----

    [Fact]
    public void AC2_four_basic_pickaxe_swings_clear_a_rock_and_add_3_stone()
    {
        var game = GameWith(NodeKind.Rock);

        var cleared = Enumerable.Range(0, 4).Select(_ => Swing(game, ToolId.Pickaxe).Cleared).ToList();

        Assert.Equal([false, false, false, true], cleared);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(3, game.Player.Backpack.Quantity(Stone));
        Assert.Equal(142, game.Player.Energy);
    }

    [Fact]
    public void BR_N3_clearing_gives_the_yield_only_on_the_clearing_swing()
    {
        var game = GameWith(NodeKind.Rock);

        for (var swing = 0; swing < 3; swing++)
        {
            Swing(game, ToolId.Pickaxe);
            Assert.Equal(0, game.Player.Backpack.Quantity(Stone));
        }

        var last = Swing(game, ToolId.Pickaxe);

        Assert.True(last.Cleared);
        Assert.Equal(3, last.Gained.Sum(gain => gain.Added));
        Assert.Equal(3, game.Player.Backpack.Quantity(Stone));
    }

    [Fact]
    public void BR_N3_clearing_a_farm_node_frees_its_tile_as_grass()
    {
        var game = GameWith(NodeKind.Tree, swingsTaken: 3);

        var result = Swing(game, ToolId.Axe);

        Assert.True(result.Cleared);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        Assert.DoesNotContain(game.ResourceNodes, node => node.Position == Target);
        Assert.Equal(TileState.Grass, game.FarmPlot[Target].State);
    }

    // ---- AC3 / BR-N2: higher tiers ----

    [Theory]
    [InlineData(ToolTier.Rosk, 3)]
    [InlineData(ToolTier.Groth, 2)]
    public void AC3_a_higher_tier_axe_put_in_a_slot_clears_a_tree_on_its_tiers_swing(ToolTier tier, int swings)
    {
        var game = GameWith(NodeKind.Tree, axe: tier);

        var cleared = Enumerable.Range(0, swings).Select(_ => Swing(game, ToolId.Axe).Cleared).ToList();

        Assert.Equal(Enumerable.Repeat(false, swings - 1).Append(true), cleared);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(150 - (swings * Cost), game.Player.Energy);
        Assert.Equal(5, game.Player.Backpack.Quantity(Wood));
        Assert.Equal(2, game.Player.Backpack.Quantity(Sap));
    }

    [Fact]
    public void BR_N2_a_rosk_pickaxe_clears_a_rock_on_the_third_swing()
    {
        var game = GameWith(NodeKind.Rock, pickaxe: ToolTier.Rosk);

        var cleared = Enumerable.Range(0, 3).Select(_ => Swing(game, ToolId.Pickaxe).Cleared).ToList();

        Assert.Equal([false, false, true], cleared);
    }

    [Fact]
    public void BR_N2_swings_already_taken_count_toward_the_tier()
    {
        var game = GameWith(NodeKind.Tree, swingsTaken: 2, axe: ToolTier.Rosk);

        Assert.True(Swing(game, ToolId.Axe).Cleared);
    }

    // ---- AC4 / BR-N1: the wrong tool, or no node ----

    [Theory]
    [InlineData(NodeKind.Tree, ToolId.Pickaxe)]
    [InlineData(NodeKind.Stump, ToolId.Pickaxe)]
    [InlineData(NodeKind.Rock, ToolId.Axe)]
    public void AC4_the_wrong_tool_at_a_node_is_NothingToDo_and_costs_nothing(NodeKind kind, ToolId tool)
    {
        var game = GameWith(kind);
        var before = State(game);

        Assert.Equal(Rejection.NothingToDo, RejectionOf(game, tool));
        var result = Swing(game, tool);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), result.Result);
        Assert.False(result.Cleared);
        Assert.Empty(result.Gained);
        Assert.Equal(before, State(game));
    }

    [Theory]
    [InlineData(ToolId.Axe)]
    [InlineData(ToolId.Pickaxe)]
    public void BR_N1_a_swing_at_a_tile_with_no_node_is_NothingToDo_and_costs_nothing(ToolId tool)
    {
        var game = GameWith(NodeKind.Tree);
        var empty = new GridPosition(0, 0);
        var before = State(game);

        var tier = game.Player.Backpack.TierOf(tool)!.Value;
        Assert.Equal(
            Rejection.NothingToDo, game.SwingRejection(AreaId.Farm, empty, tool, tier, Cost));
        var result = game.Swing(AreaId.Farm, empty, tool, tier, Cost, SwingsToClear, NodeYields, Feet);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), result.Result);
        Assert.Equal(before, State(game));
    }

    [Theory]
    [InlineData(ToolId.Hoe)]
    [InlineData(ToolId.WateringCan)]
    public void BR_N1_the_hoe_or_can_never_swings_at_a_node(ToolId tool)
    {
        var game = GameWith(NodeKind.Tree);

        Assert.Equal(
            Rejection.NothingToDo,
            game.SwingRejection(AreaId.Farm, Target, tool, ToolTier.Groth, Cost));
    }

    [Theory]
    [InlineData(NodeKind.Tree, ToolId.Axe)]
    [InlineData(NodeKind.Stump, ToolId.Axe)]
    [InlineData(NodeKind.Rock, ToolId.Pickaxe)]
    public void BR_N1_the_right_tool_at_a_node_is_allowed(NodeKind kind, ToolId tool)
    {
        var game = GameWith(kind, axe: ToolTier.Groth);

        Assert.Null(RejectionOf(game, tool));
    }

    // ---- AC5 / BR-N6: stumps need a Groth axe ----

    [Theory]
    [InlineData(ToolTier.Basic)]
    [InlineData(ToolTier.Rosk)]
    public void AC5_an_axe_below_groth_at_a_stump_is_NeedsGrothAxe_and_costs_nothing(ToolTier tier)
    {
        var game = GameWith(NodeKind.Stump, axe: tier);
        var before = State(game);

        Assert.Equal(Rejection.NeedsGrothAxe, RejectionOf(game, ToolId.Axe));
        var result = Swing(game, ToolId.Axe);

        Assert.Equal(ActionResult.Rejected(Rejection.NeedsGrothAxe), result.Result);
        Assert.Equal(before, State(game));
    }

    [Fact]
    public void AC5_a_groth_axe_clears_a_stump_in_2_swings_for_4_wood()
    {
        var game = GameWith(NodeKind.Stump, axe: ToolTier.Groth);

        var cleared = Enumerable.Range(0, 2).Select(_ => Swing(game, ToolId.Axe).Cleared).ToList();

        Assert.Equal([false, true], cleared);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(4, game.Player.Backpack.Quantity(Wood));
        Assert.Equal(0, game.Player.Backpack.Quantity(Sap));
        Assert.Equal(146, game.Player.Energy);
    }

    [Fact]
    public void BR_N6_NeedsGrothAxe_comes_before_TooTired()
    {
        var game = GameWith(NodeKind.Stump, energy: 1);

        Assert.Equal(Rejection.NeedsGrothAxe, RejectionOf(game, ToolId.Axe));
    }

    [Fact]
    public void BR_N6_a_pickaxe_at_a_stump_is_NothingToDo_not_NeedsGrothAxe()
    {
        var game = GameWith(NodeKind.Stump);

        Assert.Equal(Rejection.NothingToDo, RejectionOf(game, ToolId.Pickaxe));
    }

    // ---- AC6 / BR-P2: too tired ----

    [Theory]
    [InlineData(NodeKind.Tree, ToolId.Axe, 1)]
    [InlineData(NodeKind.Rock, ToolId.Pickaxe, 1)]
    [InlineData(NodeKind.Tree, ToolId.Axe, 0)]
    public void AC6_a_swing_with_less_than_2_energy_is_TooTired_and_changes_nothing(NodeKind kind, ToolId tool, int energy)
    {
        var game = GameWith(kind, energy: energy, swingsTaken: 3);
        var before = State(game);

        Assert.Equal(Rejection.TooTired, RejectionOf(game, tool));
        var result = Swing(game, tool);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result.Result);
        Assert.False(result.Cleared);
        Assert.Equal(before, State(game));
        Assert.Equal(3, game.NodeAt(AreaId.Farm, Target)!.SwingsTaken);
    }

    [Fact]
    public void BR_P2_NothingToDo_comes_before_TooTired()
    {
        var game = GameWith(NodeKind.Rock, energy: 0);

        Assert.Equal(Rejection.NothingToDo, RejectionOf(game, ToolId.Axe));
    }

    // ---- AC7 / BR-T2: the cleared Tile can be tilled ----

    [Fact]
    public void AC7_a_tree_tile_is_NodeInTheWay_before_clearing_and_tillable_after()
    {
        var game = GameWith(NodeKind.Tree);
        Assert.Equal(Rejection.NodeInTheWay, game.TillRejection(Target, Cost));

        for (var swing = 0; swing < 4; swing++)
        {
            Swing(game, ToolId.Axe);
        }

        Assert.Null(game.TillRejection(Target, Cost));
        Assert.Equal(ActionResult.Ok, game.Till(Target, Cost));
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
    }

    [Fact]
    public void BR_T2_a_rock_tile_is_still_NodeInTheWay_after_three_swings()
    {
        var game = GameWith(NodeKind.Rock);

        for (var swing = 0; swing < 3; swing++)
        {
            Swing(game, ToolId.Pickaxe);
        }

        Assert.Equal(Rejection.NodeInTheWay, game.TillRejection(Target, Cost));
    }

    [Fact]
    public void BR_T2_a_cleared_stump_tile_can_be_tilled()
    {
        var game = GameWith(NodeKind.Stump, axe: ToolTier.Groth, swingsTaken: 1);

        Assert.True(Swing(game, ToolId.Axe).Cleared);

        Assert.Equal(ActionResult.Ok, game.Till(Target, Cost));
    }

    // ---- AC8 / BR-K3: the yield goes through Gain ----

    [Fact]
    public void AC8_a_yield_tops_up_the_stack_then_the_lowest_free_slot_and_drops_the_rest_at_the_feet()
    {
        // Slot 2 the axe, Slot 3 97 wood, Slot 7 free, every other Slot full of filler: 2 wood top up Slot 3, 3 go to
        // Slot 7, and the 2 sap fit nowhere.
        var slots = new List<BackpackSlot>
        {
            new(2, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic),
            new(3, Wood, 97, null),
        };
        slots.AddRange(new[] { 4, 5, 6, 8, 9, 10, 11 }.Select(index => new BackpackSlot(index, TestGames.Filler, 99, null)));
        var game = NodeGames.WithNodes(
            TestGames.WithBackpack(TestGames.RestoredBackpack([.. slots])), NodeGames.Node(Target.X, Target.Y, swingsTaken: 3));

        var result = Swing(game, ToolId.Axe);

        Assert.True(result.Cleared);
        var backpack = game.Player.Backpack;
        Assert.Equal((Wood, 99), (backpack.Slots[3].Item!.Value, backpack.Slots[3].Quantity));
        Assert.Equal((Wood, 3), (backpack.Slots[7].Item!.Value, backpack.Slots[7].Quantity));
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal((Sap, 2, Feet, AreaId.Farm), (dropped.Item, dropped.Quantity, dropped.Position, dropped.Area));
    }

    [Fact]
    public void BR_K3_a_yield_with_no_room_at_all_drops_whole_at_the_feet()
    {
        var game = NodeGames.WithNodes(
            TestGames.WithBackpack(TestGames.FullBackpack()), NodeGames.Node(Target.X, Target.Y, NodeKind.Rock, swingsTaken: 3));

        var result = game.Swing(AreaId.Farm, Target, ToolId.Pickaxe, ToolTier.Basic, Cost, SwingsToClear, NodeYields, Feet);

        Assert.True(result.Cleared);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal((Stone, 3, Feet), (dropped.Item, dropped.Quantity, dropped.Position));
        var gain = Assert.Single(result.Gained);
        Assert.Equal(0, gain.Added);
        Assert.Single(gain.Dropped);
    }

    [Fact]
    public void BR_K3_a_yield_joins_a_stack_of_the_same_material_before_a_free_slot()
    {
        var game = NodeGames.WithNodes(
            TestGames.WithBackpack(TestGames.RestoredBackpack(
                new BackpackSlot(2, ItemRef.Tool(ToolId.Pickaxe), 1, ToolTier.Basic),
                new BackpackSlot(9, Stone, 10, null))),
            NodeGames.Node(Target.X, Target.Y, NodeKind.Rock, swingsTaken: 3));

        Swing(game, ToolId.Pickaxe);

        Assert.Equal(13, game.Player.Backpack.Slots[9].Quantity);
        Assert.Null(game.Player.Backpack.Slots[3].Item);
    }

    // ---- ResetSwings (BR-N2) ----

    [Fact]
    public void BR_N2_ResetSwings_forgets_the_swings_on_every_node()
    {
        var game = NodeGames.WithEnergyAndNodes(
            150,
            NodeGames.Node(1, 1, NodeKind.Tree, swingsTaken: 2),
            NodeGames.Node(2, 2, NodeKind.Rock, swingsTaken: 3),
            NodeGames.Node(3, 3, NodeKind.Stump, swingsTaken: 1),
            NodeGames.Node(4, 4, NodeKind.Tree));

        game.ResetSwings();

        Assert.All(game.ResourceNodes, node => Assert.Equal(0, node.SwingsTaken));
        Assert.Equal(4, game.ResourceNodes.Count);
    }
}
