using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #152 through <see cref="GameSession"/>: with the axe held at a tree, Use previews and performs <c>Chop</c>; with the
/// pickaxe at a rock, <c>Break</c> (BR-N1). The tier is the held Slot's <see cref="ToolTier"/>. Clearing raises
/// NodesChanged and TileChanged for the freed Tile; a refused swing changes nothing and raises nothing. The Use prompt
/// reads <c>Chop</c>, <c>Break</c>, "Needs a Groth axe" (AC11). A held material does nothing on Use.
/// </summary>
public class SwingSessionTests
{
    private static readonly GridPosition Target = new(10, 20);

    private static readonly GridPosition Feet = new(10, 21);

    /// <summary>The axe's hotbar position (Slot 2) and the pickaxe's (Slot 3).</summary>
    private const int AxePosition = 2;

    private const int PickaxePosition = 3;

    private static GameSession SessionWith(
        NodeKind kind, int energy = 150, ToolTier axe = ToolTier.Basic, int swingsTaken = 0, params BackpackSlot[] extra) =>
        new(
            NodeGames.WithNodes(
                TestGames.WithBackpack(
                    TestGames.RestoredBackpack(
                        [
                            new BackpackSlot(AxePosition, ItemRef.Tool(ToolId.Axe), 1, axe),
                            new BackpackSlot(PickaxePosition, ItemRef.Tool(ToolId.Pickaxe), 1, ToolTier.Basic),
                            .. extra,
                        ]),
                    energy: energy),
                NodeGames.Node(Target.X, Target.Y, kind, swingsTaken)),
            TestGames.Shipped);

    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.NodesChanged += () => events.Add("NodesChanged");
        session.TileChanged += position => events.Add($"TileChanged:{position}");
        session.PlayerChanged += () => events.Add("PlayerChanged");
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.DroppedItemsChanged += () => events.Add("DroppedItemsChanged");
        return events;
    }

    // ---- Preview ----

    [Fact]
    public void AC1_preview_with_the_axe_at_a_tree_is_Chop()
    {
        var session = SessionWith(NodeKind.Tree);

        Assert.Equal(new UsePreview(UseActionKind.Chop, null), session.PreviewUse(Target, AxePosition));
    }

    [Fact]
    public void AC2_preview_with_the_pickaxe_at_a_rock_is_Break()
    {
        var session = SessionWith(NodeKind.Rock);

        Assert.Equal(new UsePreview(UseActionKind.Break, null), session.PreviewUse(Target, PickaxePosition));
    }

    [Theory]
    [InlineData(NodeKind.Tree, PickaxePosition)]
    [InlineData(NodeKind.Rock, AxePosition)]
    public void AC4_preview_with_the_wrong_tool_is_NothingToDo(NodeKind kind, int position)
    {
        var session = SessionWith(kind);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, position));
    }

    [Fact]
    public void AC5_preview_with_a_basic_axe_at_a_stump_is_NeedsGrothAxe()
    {
        var session = SessionWith(NodeKind.Stump);

        Assert.Equal(new UsePreview(null, Rejection.NeedsGrothAxe), session.PreviewUse(Target, AxePosition));
    }

    [Fact]
    public void AC5_preview_with_a_groth_axe_in_the_held_slot_at_a_stump_is_Chop()
    {
        var session = SessionWith(NodeKind.Stump, axe: ToolTier.Groth);

        Assert.Equal(new UsePreview(UseActionKind.Chop, null), session.PreviewUse(Target, AxePosition));
    }

    [Fact]
    public void AC6_preview_with_1_energy_is_TooTired()
    {
        var session = SessionWith(NodeKind.Tree, energy: 1);

        Assert.Equal(new UsePreview(null, Rejection.TooTired), session.PreviewUse(Target, AxePosition));
    }

    // ---- Use ----

    [Fact]
    public void AC1_use_with_the_axe_chops_the_tree_and_the_fourth_clears_it_into_the_backpack()
    {
        var session = SessionWith(NodeKind.Tree);

        for (var swing = 1; swing <= 3; swing++)
        {
            Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition, Feet));
            Assert.Equal(swing, session.Game.NodeAt(AreaId.Farm, Target)!.SwingsTaken);
        }

        Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition, Feet));

        Assert.Null(session.Game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(142, session.Game.Player.Energy);
        Assert.Equal(5, session.Game.Player.Backpack.Quantity(ItemRef.Material(MaterialId.Wood)));
        Assert.Equal(2, session.Game.Player.Backpack.Quantity(ItemRef.Material(MaterialId.Sap)));
    }

    [Fact]
    public void AC3_use_reads_the_tier_from_the_held_slot_so_a_rosk_axe_clears_a_tree_on_the_third_swing()
    {
        var session = SessionWith(NodeKind.Tree, axe: ToolTier.Rosk);

        session.Use(Target, AxePosition, Feet);
        session.Use(Target, AxePosition, Feet);
        Assert.NotNull(session.Game.NodeAt(AreaId.Farm, Target));

        Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition, Feet));

        Assert.Null(session.Game.NodeAt(AreaId.Farm, Target));
    }

    [Fact]
    public void AC2_use_with_the_pickaxe_clears_a_rock_with_three_swings_taken_for_3_stone()
    {
        var session = SessionWith(NodeKind.Rock, swingsTaken: 3);

        Assert.Equal(ActionResult.Ok, session.Use(Target, PickaxePosition, Feet));

        Assert.Null(session.Game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(3, session.Game.Player.Backpack.Quantity(ItemRef.Material(MaterialId.Stone)));
    }

    [Fact]
    public void Use_that_clears_a_node_raises_NodesChanged_and_TileChanged_for_its_tile()
    {
        var session = SessionWith(NodeKind.Tree, swingsTaken: 3);
        var events = RecordEvents(session);

        session.Use(Target, AxePosition, Feet);

        Assert.Single(events, name => name == "NodesChanged");
        Assert.Contains($"TileChanged:{Target}", events);
        Assert.Contains("PlayerChanged", events);
        Assert.Contains("InventoryChanged", events);
    }

    [Fact]
    public void Use_that_does_not_clear_raises_PlayerChanged_but_not_NodesChanged_or_TileChanged()
    {
        var session = SessionWith(NodeKind.Tree);
        var events = RecordEvents(session);

        Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition, Feet));

        Assert.Contains("PlayerChanged", events);
        Assert.DoesNotContain("NodesChanged", events);
        Assert.DoesNotContain(events, name => name.StartsWith("TileChanged", StringComparison.Ordinal));
        Assert.Equal(1, session.Game.NodeAt(AreaId.Farm, Target)!.SwingsTaken);
    }

    [Fact]
    public void AC8_use_that_clears_into_a_full_backpack_drops_the_yield_at_the_feet_and_raises_DroppedItemsChanged()
    {
        var full = TestGames.FullBackpack();
        full.RemoveFrom(2, 99);
        full.RemoveFrom(3, 99);
        Assert.True(full.AddTool(ToolId.Axe, ToolTier.Basic));
        Assert.True(full.AddTool(ToolId.Pickaxe, ToolTier.Basic));
        var session = new GameSession(
            NodeGames.WithNodes(TestGames.WithBackpack(full), NodeGames.Node(Target.X, Target.Y, NodeKind.Rock, swingsTaken: 3)),
            TestGames.Shipped);
        var events = RecordEvents(session);

        Assert.Equal(ActionResult.Ok, session.Use(Target, PickaxePosition, Feet));

        var dropped = Assert.Single(session.Game.DroppedItems);
        Assert.Equal((ItemRef.Material(MaterialId.Stone), 3, Feet), (dropped.Item, dropped.Quantity, dropped.Position));
        Assert.Contains("DroppedItemsChanged", events);
    }

    [Theory]
    [InlineData(NodeKind.Stump, 150, Rejection.NeedsGrothAxe)]
    [InlineData(NodeKind.Tree, 1, Rejection.TooTired)]
    [InlineData(NodeKind.Rock, 150, Rejection.NothingToDo)]
    public void AC6_a_refused_use_changes_nothing_and_raises_nothing(NodeKind kind, int energy, Rejection expected)
    {
        var session = SessionWith(kind, energy: energy, swingsTaken: 1);
        var before = (TestGames.FullSnapshot(session.Game), NodeGames.Describe(session.ResourceNodes));
        var events = RecordEvents(session);

        var result = session.Use(Target, AxePosition, Feet);

        Assert.Equal(ActionResult.Rejected(expected), result);
        Assert.Equal(before.Item1, TestGames.FullSnapshot(session.Game));
        Assert.Equal(before.Item2, NodeGames.Describe(session.ResourceNodes));
        Assert.Empty(events);
    }

    [Fact]
    public void AC7_after_chopping_a_tree_down_the_hoe_tills_its_tile()
    {
        var session = SessionWith(NodeKind.Tree, swingsTaken: 3);
        Assert.Equal(new UsePreview(null, Rejection.NodeInTheWay), session.PreviewUse(Target, 0));

        session.Use(Target, AxePosition, Feet);

        Assert.Equal(new UsePreview(UseActionKind.Till, null), session.PreviewUse(Target, 0));
        Assert.Equal(ActionResult.Ok, session.Use(Target, 0, Feet));
        Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Target].State);
    }

    [Fact]
    public void Swinging_off_the_farm_is_NoTarget()
    {
        var session = SessionWith(NodeKind.Tree);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(Target, AxePosition));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Use(Target, AxePosition, Feet));
    }

    [Fact]
    public void The_existing_use_order_holds_harvest_still_comes_before_the_held_axe()
    {
        // A Ripe Planting on a Tile with no node: harvest whatever is held (BR-T6), the axe included.
        var spot = new GridPosition(5, 5);
        var session = new GameSession(
            TestGames.WithBackpack(
                TestGames.RestoredBackpack(new BackpackSlot(AxePosition, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic)),
                tiles: TestGames.TilledWith(spot, TestGames.Ripe(Domain.Catalog.CropTypeId.Emberroot))),
            TestGames.Shipped);

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), session.PreviewUse(spot, AxePosition));
    }

    // ---- A held material ----

    [Fact]
    public void A_held_material_is_the_held_item_and_does_nothing_on_use()
    {
        var session = SessionWith(NodeKind.Tree, extra: new BackpackSlot(4, ItemRef.Material(MaterialId.Wood), 5, null));
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(HeldItem.MaterialOf(MaterialId.Wood), session.HeldItemAt(4));
        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, 4));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, 4, Feet));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    // ---- AC11: the Use prompt ----

    [Fact]
    public void AC11_the_use_prompt_reads_Chop_at_a_tree_with_the_axe()
    {
        var session = SessionWith(NodeKind.Tree);

        var prompt = UsePromptText.For(
            session.PreviewUse(Target, AxePosition), session.HeldItemAt(AxePosition), null, session.Data.CropTypes);

        Assert.Equal(("Chop", true), prompt);
    }

    [Fact]
    public void AC11_the_use_prompt_reads_Break_at_a_rock_with_the_pickaxe()
    {
        var session = SessionWith(NodeKind.Rock);

        var prompt = UsePromptText.For(
            session.PreviewUse(Target, PickaxePosition), session.HeldItemAt(PickaxePosition), null, session.Data.CropTypes);

        Assert.Equal(("Break", true), prompt);
    }

    [Fact]
    public void AC11_the_use_prompt_at_a_stump_with_a_basic_axe_reads_Needs_a_Groth_axe()
    {
        var prompt = UsePromptText.For(
            new UsePreview(null, Rejection.NeedsGrothAxe), HeldItem.Of(ToolId.Axe), null, TestGames.Shipped.CropTypes);

        Assert.Equal(("Needs a Groth axe", false), prompt);
    }
}
