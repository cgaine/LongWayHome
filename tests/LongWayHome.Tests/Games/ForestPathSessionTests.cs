using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #154 through <see cref="GameSession"/> once the Player has entered the forest path: the axe and pickaxe swing at
/// that Area's nodes (AC2, BR-N2, BR-N3, BR-P2); the hoe, the can and seeds have no target and a wrong tool is
/// NothingToDo (AC3, BR-N1); yield overflow drops on the forest path at the feet, shows only there and is picked up
/// there (AC4, BR-K3, BR-K4).
/// </summary>
public class ForestPathSessionTests
{
    /// <summary>A forest path Tile inside both grids, so it is written with the farm's GridPosition constructor.</summary>
    private static readonly GridPosition Target = new(10, 12);

    private static readonly GridPosition Feet = new(10, 13);

    private const int HoePosition = 0;

    private const int CanPosition = 1;

    private const int AxePosition = 2;

    private const int PickaxePosition = 3;

    private const int SeedPosition = 4;

    /// <summary>A ResourceNode at a forest path spawn point, id <c>forest_path-&lt;x&gt;-&lt;y&gt;</c>.</summary>
    private static ResourceNode ForestNode(GridPosition position, NodeKind kind, int swingsTaken = 0) =>
        new($"forest_path-{position.X}-{position.Y}", kind, AreaId.ForestPath, position, swingsTaken);

    /// <summary>The hoe, the can, a Basic axe (Slot 2), a Basic pickaxe (Slot 3) and 5 emberroot seeds (Slot 4).</summary>
    private static Backpack Tools() =>
        TestGames.RestoredBackpack(
            new BackpackSlot(AxePosition, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic),
            new BackpackSlot(PickaxePosition, ItemRef.Tool(ToolId.Pickaxe), 1, ToolTier.Basic),
            new BackpackSlot(SeedPosition, ItemRef.Seed(CropTypeId.Emberroot), 5, null));

    /// <summary>A session at 150 energy holding <see cref="Tools"/> with the given nodes, already on the forest path.</summary>
    private static GameSession OnForestPath(Backpack backpack, params ResourceNode[] nodes)
    {
        var session = new GameSession(
            NodeGames.WithNodes(TestGames.WithBackpack(backpack), nodes), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.ForestPath));
        return session;
    }

    private static GameSession OnForestPath(params ResourceNode[] nodes) => OnForestPath(Tools(), nodes);

    private static int Quantity(GameSession session, MaterialId material) =>
        session.Game.Player.Backpack.Quantity(ItemRef.Material(material));

    // ---- AC2 ----

    [Fact]
    public void AC2_preview_with_the_axe_at_a_forest_path_tree_is_Chop()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Tree));

        Assert.Equal(new UsePreview(UseActionKind.Chop, null), session.PreviewUse(Target, AxePosition));
    }

    [Fact]
    public void AC2_preview_with_the_pickaxe_at_a_forest_path_rock_is_Break()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Rock));

        Assert.Equal(new UsePreview(UseActionKind.Break, null), session.PreviewUse(Target, PickaxePosition));
    }

    [Fact]
    public void AC2_four_basic_axe_swings_at_a_forest_path_tree_spend_8_energy_and_give_5_wood_and_2_sap()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Tree));

        for (var swing = 1; swing <= 4; swing++)
        {
            Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition, Feet));
        }

        Assert.Null(session.Game.NodeAt(AreaId.ForestPath, Target));
        Assert.Equal(142, session.Game.Player.Energy);
        Assert.Equal(5, Quantity(session, MaterialId.Wood));
        Assert.Equal(2, Quantity(session, MaterialId.Sap));
    }

    [Fact]
    public void AC2_four_basic_pickaxe_swings_at_a_forest_path_bank_rock_give_3_stone()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Rock));

        for (var swing = 1; swing <= 4; swing++)
        {
            Assert.Equal(ActionResult.Ok, session.Use(Target, PickaxePosition, Feet));
        }

        Assert.Null(session.Game.NodeAt(AreaId.ForestPath, Target));
        Assert.Equal(142, session.Game.Player.Energy);
        Assert.Equal(3, Quantity(session, MaterialId.Stone));
    }

    [Fact]
    public void AC2_a_swing_that_does_not_clear_counts_on_the_forest_path_node()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Tree));

        Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition, Feet));

        Assert.Equal(1, session.Game.NodeAt(AreaId.ForestPath, Target)!.SwingsTaken);
        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Fact]
    public void AC2_Swing_on_the_forest_path_swings_at_that_areas_node()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Rock, swingsTaken: 3));

        Assert.Equal(ActionResult.Ok, session.Swing(Target, ToolId.Pickaxe, Feet));

        Assert.Null(session.Game.NodeAt(AreaId.ForestPath, Target));
        Assert.Equal(3, Quantity(session, MaterialId.Stone));
    }

    [Fact]
    public void AC2_a_tree_on_the_forest_paths_south_rows_beyond_the_farms_grid_can_be_chopped()
    {
        var south = AreaGrid.Position(AreaId.ForestPath, 2, 38);
        var session = OnForestPath(ForestNode(south, NodeKind.Tree, swingsTaken: 3));

        Assert.Equal(ActionResult.Ok, session.Use(south, AxePosition, Feet));

        Assert.Null(session.Game.NodeAt(AreaId.ForestPath, south));
        Assert.Equal(5, Quantity(session, MaterialId.Wood));
    }

    [Fact]
    public void AC2_on_the_forest_path_the_axe_does_not_reach_a_farm_tree_on_the_same_tile()
    {
        var session = OnForestPath(NodeGames.Node(Target.X, Target.Y, NodeKind.Tree));

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, AxePosition));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, AxePosition, Feet));
        Assert.NotNull(session.Game.NodeAt(AreaId.Farm, Target));
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Fact]
    public void AC2_on_the_farm_the_axe_does_not_reach_a_forest_path_tree_on_the_same_tile()
    {
        var session = new GameSession(
            NodeGames.WithNodes(TestGames.WithBackpack(Tools()), ForestNode(Target, NodeKind.Tree)), TestGames.Shipped);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, AxePosition));
        Assert.NotNull(session.Game.NodeAt(AreaId.ForestPath, Target));
    }

    // ---- AC3 / BR-N1 ----

    [Theory]
    [InlineData(HoePosition)]
    [InlineData(CanPosition)]
    [InlineData(SeedPosition)]
    public void AC3_on_the_forest_path_the_hoe_the_can_and_seeds_have_no_target(int position)
    {
        var session = OnForestPath(ForestNode(Feet, NodeKind.Tree));
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(Target, position));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Use(Target, position, Feet));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC3_on_the_forest_path_the_axe_at_a_rock_is_NothingToDo_and_costs_nothing()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Rock));

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, AxePosition));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, AxePosition, Feet));
        Assert.Equal(150, session.Game.Player.Energy);
        Assert.Equal(0, session.Game.NodeAt(AreaId.ForestPath, Target)!.SwingsTaken);
    }

    [Fact]
    public void AC3_on_the_forest_path_the_pickaxe_at_a_tree_is_NothingToDo()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Tree));

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, PickaxePosition));
    }

    [Fact]
    public void AC3_on_the_forest_path_the_axe_at_an_empty_tile_is_NothingToDo()
    {
        var session = OnForestPath();

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, AxePosition));
    }

    [Fact]
    public void AC3_on_the_forest_path_with_no_target_the_axe_has_no_target()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Tree));

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(null, AxePosition));
    }

    [Fact]
    public void AC3_in_town_the_axe_still_has_no_target()
    {
        var session = OnForestPath(ForestNode(Target, NodeKind.Tree));
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(Target, AxePosition));
    }

    // ---- AC4 / BR-K3 / BR-K4 ----

    /// <summary>A full Backpack whose free Slots hold the filler, with the axe and pickaxe at Slots 2 and 3.</summary>
    private static Backpack FullWithTools()
    {
        var full = TestGames.FullBackpack();
        full.RemoveFrom(2, 99);
        full.RemoveFrom(3, 99);
        Assert.True(full.AddTool(ToolId.Axe, ToolTier.Basic));
        Assert.True(full.AddTool(ToolId.Pickaxe, ToolTier.Basic));
        return full;
    }

    [Fact]
    public void AC4_a_yield_that_does_not_fit_drops_on_the_forest_path_at_the_feet()
    {
        var session = OnForestPath(FullWithTools(), ForestNode(Target, NodeKind.Rock, swingsTaken: 3));

        Assert.Equal(ActionResult.Ok, session.Use(Target, PickaxePosition, Feet));

        var dropped = Assert.Single(session.Game.DroppedItems);
        Assert.Equal(
            (AreaId.ForestPath, ItemRef.Material(MaterialId.Stone), 3, Feet),
            (dropped.Area, dropped.Item, dropped.Quantity, dropped.Position));
    }

    [Fact]
    public void AC4_the_dropped_yield_shows_on_the_forest_path_but_not_on_the_farm()
    {
        var session = OnForestPath(FullWithTools(), ForestNode(Target, NodeKind.Rock, swingsTaken: 3));
        session.Use(Target, PickaxePosition, Feet);

        Assert.True(session.HasDroppedItemsAt(Feet));
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Farm));
        Assert.False(session.HasDroppedItemsAt(Feet));
        Assert.False(session.PickUp(Feet));
        Assert.Single(session.Game.DroppedItems);
    }

    [Fact]
    public void AC4_the_dropped_yield_can_be_picked_up_on_the_forest_path_once_there_is_room()
    {
        var session = OnForestPath(FullWithTools(), ForestNode(Target, NodeKind.Rock, swingsTaken: 3));
        session.Use(Target, PickaxePosition, Feet);
        session.Game.Player.Backpack.RemoveFrom(4, 99);

        Assert.True(session.PickUp(Feet));

        Assert.Empty(session.Game.DroppedItems);
        Assert.Equal(3, Quantity(session, MaterialId.Stone));
    }

    [Fact]
    public void AC4_Game_Gain_drops_what_does_not_fit_in_the_given_area()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack());

        var result = game.Gain(ItemRef.Material(MaterialId.Wood), 5, Feet, AreaId.ForestPath);

        var dropped = Assert.Single(result.Dropped);
        Assert.Equal(AreaId.ForestPath, dropped.Area);
        Assert.Equal(AreaId.ForestPath, Assert.Single(game.DroppedItems).Area);
    }

    [Fact]
    public void AC4_Game_Gain_still_drops_on_the_farm_by_default()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack());

        game.Gain(ItemRef.Material(MaterialId.Wood), 5, Feet);

        Assert.Equal(AreaId.Farm, Assert.Single(game.DroppedItems).Area);
    }

    [Theory]
    [InlineData(5, 20, 5, 20)]
    [InlineData(13, 39, 13, 39)]
    [InlineData(14, 10, 13, 10)]
    [InlineData(30, 10, 13, 10)]
    [InlineData(-2, 38, 0, 38)]
    [InlineData(6, 40, 6, 39)]
    [InlineData(6, -5, 6, 0)]
    [InlineData(int.MaxValue, int.MaxValue, 13, 39)]
    public void AC4_feet_on_the_forest_path_clamp_to_its_14_by_40_grid(int cellX, int cellY, int x, int y)
    {
        var feet = FeetTile.Nearest(AreaId.ForestPath, cellX, cellY);

        Assert.Equal((x, y), (feet.X, feet.Y));
    }

    [Theory]
    [InlineData(5, 4, 5, 4)]
    [InlineData(80, 40, 71, 35)]
    [InlineData(-1, -1, 0, 0)]
    public void AC4_feet_on_the_farm_still_clamp_to_its_72_by_36_grid(int cellX, int cellY, int x, int y)
    {
        Assert.Equal(new GridPosition(x, y), FeetTile.Nearest(AreaId.Farm, cellX, cellY));
    }
}
