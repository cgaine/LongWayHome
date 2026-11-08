using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #152 paths the acceptance tests don't drive: bad swing tuning is refused before anything changes, the pickaxe
/// works on ore deposits and debris but nothing swings at a glimmercap (BR-N1), a node with no yield row clears for
/// nothing, the session's Swing guards, NodeSwung for the view's shake, and the material labels and icons.
/// </summary>
public class SwingGuardTests
{
    private const int Cost = 2;

    private static readonly GridPosition Target = new(10, 20);

    private static readonly GridPosition Feet = new(10, 21);

    private static readonly IReadOnlyDictionary<ToolTier, int> SwingsToClear = new Dictionary<ToolTier, int>
    {
        [ToolTier.Basic] = 4,
        [ToolTier.Rosk] = 3,
        [ToolTier.Groth] = 2,
    };

    private static readonly IReadOnlyDictionary<NodeKind, IReadOnlyList<MaterialStack>> NodeYields =
        new Dictionary<NodeKind, IReadOnlyList<MaterialStack>>
        {
            [NodeKind.Tree] = [new MaterialStack(MaterialId.Wood, 5), new MaterialStack(MaterialId.Sap, 2)],
        };

    private static GameState GameWith(NodeKind kind, int swingsTaken = 0) =>
        NodeGames.WithNodes(
            TestGames.WithBackpack(
                TestGames.RestoredBackpack(
                    new BackpackSlot(2, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic),
                    new BackpackSlot(3, ItemRef.Tool(ToolId.Pickaxe), 1, ToolTier.Basic))),
            NodeGames.Node(Target.X, Target.Y, kind, swingsTaken));

    private static (int, string, string, string, int) State(GameState game)
    {
        var (energy, tiles, inventory) = TestGames.FullSnapshot(game);
        return (energy, tiles, inventory, string.Join(",", NodeGames.Describe(game.ResourceNodes)), game.DroppedItems.Count);
    }

    // ---- Game.Swing guards ----

    [Fact]
    public void Swing_with_null_tuning_throws()
    {
        var game = GameWith(NodeKind.Tree);

        Assert.Throws<ArgumentNullException>(() =>
            game.Swing(AreaId.Farm, Target, ToolId.Axe, ToolTier.Basic, Cost, null!, NodeYields, Feet));
        Assert.Throws<ArgumentNullException>(() =>
            game.Swing(AreaId.Farm, Target, ToolId.Axe, ToolTier.Basic, Cost, SwingsToClear, null!, Feet));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Swing_with_no_count_of_at_least_1_for_the_tier_throws_and_changes_nothing(bool zero)
    {
        var game = GameWith(NodeKind.Tree);
        var before = State(game);
        IReadOnlyDictionary<ToolTier, int> counts = zero
            ? new Dictionary<ToolTier, int> { [ToolTier.Basic] = 0 }
            : new Dictionary<ToolTier, int> { [ToolTier.Groth] = 2 };

        Assert.Throws<ArgumentException>(() =>
            game.Swing(AreaId.Farm, Target, ToolId.Axe, ToolTier.Basic, Cost, counts, NodeYields, Feet));
        Assert.Equal(before, State(game));
    }

    [Fact]
    public void Swing_with_a_yield_stack_below_1_throws_and_changes_nothing()
    {
        var game = GameWith(NodeKind.Tree, swingsTaken: 3);
        var before = State(game);
        var yields = new Dictionary<NodeKind, IReadOnlyList<MaterialStack>>
        {
            [NodeKind.Tree] = [new MaterialStack(MaterialId.Wood, 0)],
        };

        Assert.Throws<ArgumentException>(() =>
            game.Swing(AreaId.Farm, Target, ToolId.Axe, ToolTier.Basic, Cost, SwingsToClear, yields, Feet));
        Assert.Equal(before, State(game));
    }

    [Fact]
    public void BR_N3_a_node_with_no_yield_row_clears_and_gains_nothing()
    {
        var game = GameWith(NodeKind.Rock, swingsTaken: 3);

        var result = game.Swing(AreaId.Farm, Target, ToolId.Pickaxe, ToolTier.Basic, Cost, SwingsToClear, NodeYields, Feet);

        Assert.Equal((ActionResult.Ok, true), (result.Result, result.Cleared));
        Assert.Empty(result.Gained);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));
    }

    [Theory]
    [InlineData(NodeKind.RoskDeposit)]
    [InlineData(NodeKind.GrothDeposit)]
    [InlineData(NodeKind.Debris)]
    public void BR_N1_the_pickaxe_works_on_ore_deposits_and_debris_and_the_axe_does_not(NodeKind kind)
    {
        var game = GameWith(kind);

        Assert.Null(game.SwingRejection(AreaId.Farm, Target, ToolId.Pickaxe, ToolTier.Basic, Cost));
        Assert.Equal(Rejection.NothingToDo, game.SwingRejection(AreaId.Farm, Target, ToolId.Axe, ToolTier.Basic, Cost));
    }

    [Theory]
    [InlineData(ToolId.Axe)]
    [InlineData(ToolId.Pickaxe)]
    public void BR_N1_a_glimmercap_is_picked_by_hand_so_no_tool_swings_at_it(ToolId tool)
    {
        var game = GameWith(NodeKind.Glimmercap);

        Assert.Equal(Rejection.NothingToDo, game.SwingRejection(AreaId.Farm, Target, tool, ToolTier.Groth, Cost));
    }

    [Fact]
    public void BR_N1_a_swing_at_the_same_tile_in_another_area_is_NothingToDo()
    {
        var game = GameWith(NodeKind.Tree);

        Assert.Equal(Rejection.NothingToDo, game.SwingRejection(AreaId.Town, Target, ToolId.Axe, ToolTier.Basic, Cost));
    }

    // ---- GameSession.Swing ----

    [Fact]
    public void Session_swing_with_a_tool_not_in_the_backpack_is_NothingToDo()
    {
        var session = new GameSession(
            NodeGames.WithNodes(TestGames.WithBackpack(TestGames.RestoredBackpack()), NodeGames.Node(Target.X, Target.Y)),
            TestGames.Shipped);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Swing(Target, ToolId.Axe, Feet));
        Assert.Equal(0, session.Game.NodeAt(AreaId.Farm, Target)!.SwingsTaken);
    }

    [Fact]
    public void Session_swing_off_the_farm_is_NoTarget()
    {
        var session = new GameSession(GameWith(NodeKind.Tree), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));

        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Swing(Target, ToolId.Axe, Feet));
    }

    [Fact]
    public void Session_swing_with_no_feet_drops_overflow_on_the_nodes_tile()
    {
        var full = TestGames.FullBackpack();
        full.RemoveFrom(3, 99);
        Assert.True(full.AddTool(ToolId.Pickaxe, ToolTier.Basic));
        var session = new GameSession(
            NodeGames.WithNodes(TestGames.WithBackpack(full), NodeGames.Node(Target.X, Target.Y, NodeKind.Rock, swingsTaken: 3)),
            TestGames.Shipped);

        Assert.Equal(ActionResult.Ok, session.Swing(Target, ToolId.Pickaxe));

        Assert.Equal(Target, Assert.Single(session.Game.DroppedItems).Position);
    }

    [Fact]
    public void NodeSwung_is_raised_with_the_tile_on_a_swing_that_leaves_the_node_standing_and_not_on_a_clear()
    {
        var session = new GameSession(GameWith(NodeKind.Tree, swingsTaken: 2), TestGames.Shipped);
        var swung = new List<GridPosition>();
        session.NodeSwung += swung.Add;

        session.Swing(Target, ToolId.Axe, Feet);
        Assert.Equal([Target], swung);

        session.Swing(Target, ToolId.Axe, Feet);
        Assert.Equal([Target], swung);
        Assert.Null(session.Game.NodeAt(AreaId.Farm, Target));
    }

    [Fact]
    public void NodeSwung_is_not_raised_by_a_refused_swing()
    {
        var session = new GameSession(GameWith(NodeKind.Stump), TestGames.Shipped);
        var swung = new List<GridPosition>();
        session.NodeSwung += swung.Add;

        Assert.Equal(ActionResult.Rejected(Rejection.NeedsGrothAxe), session.Swing(Target, ToolId.Axe, Feet));
        Assert.Empty(swung);
    }

    [Fact]
    public void Session_with_no_swing_count_for_the_held_tier_offers_no_swing_and_does_nothing()
    {
        var data = TestGames.Shipped with
        {
            Tuning = TestGames.Shipped.Tuning with { SwingsToClear = new Dictionary<ToolTier, int>() },
        };
        var session = new GameSession(GameWith(NodeKind.Tree), data);
        var before = State(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, 2, Feet));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Swing(Target, ToolId.Axe, Feet));
        Assert.Equal(before, State(session.Game));
    }

    [Fact]
    public void The_held_label_of_a_material_shows_the_total_across_slots()
    {
        var session = TestGames.SessionWithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(4, ItemRef.Material(MaterialId.Wood), 99, null),
            new BackpackSlot(7, ItemRef.Material(MaterialId.Wood), 12, null)));

        Assert.Equal("Wood × 111", session.HeldLabel(HeldItem.MaterialOf(MaterialId.Wood)));
    }

    // ---- Labels and icons ----

    [Fact]
    public void The_rosk_pump_has_no_icon_yet()
    {
        Assert.Null(ItemIcons.MaterialIcon(MaterialId.RoskPump));
        Assert.Null(ItemIcons.For(ItemRef.Material(MaterialId.RoskPump)));
    }

    [Fact]
    public void A_held_materials_icon_is_its_material_icon()
    {
        Assert.Equal("res://art/icons/material_wood.svg", ItemIcons.For(HeldItem.MaterialOf(MaterialId.Wood)));
    }

    [Fact]
    public void An_undefined_material_has_no_label_or_icon()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaterialLabels.For((MaterialId)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemIcons.MaterialIcon((MaterialId)99));
    }

    [Fact]
    public void A_held_item_with_a_tool_and_a_material_has_no_label()
    {
        Assert.Throws<InvalidOperationException>(() => new HeldItem(ToolId.Axe, null, MaterialId.Wood).DisplayLabel);
    }

    [Theory]
    [InlineData(UseActionKind.Chop, "Chop")]
    [InlineData(UseActionKind.Break, "Break")]
    public void The_chop_and_break_prompts_name_the_action(UseActionKind action, string text)
    {
        Assert.Equal(
            (text, true),
            UsePromptText.For(new UsePreview(action, null), HeldItem.Of(ToolId.Axe), null, TestGames.Shipped.CropTypes));
    }
}
