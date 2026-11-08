using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #154 code paths around the red tests: the per-Area mapping (<see cref="AreaGridLayout"/>) and tool target
/// (<see cref="UseTarget"/>) the Godot glue uses, feet clamping off the forest path, DroppedItem positions checked
/// against their Area's grid, GridPosition's equality and deconstruction, and the session's forest path swings
/// (harvest never runs there, a clearing raises no farm TileChanged, the yield drops on the node's Tile when no feet
/// are given, and town has no swings).
/// </summary>
public class ForestPathGlueTests
{
    private const int AxePosition = 2;

    private const int PickaxePosition = 3;

    private static readonly GridPosition Target = new(10, 12);

    private static Backpack Tools() =>
        TestGames.RestoredBackpack(
            new BackpackSlot(AxePosition, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic),
            new BackpackSlot(PickaxePosition, ItemRef.Tool(ToolId.Pickaxe), 1, ToolTier.Basic));

    private static ResourceNode ForestNode(GridPosition position, NodeKind kind, int swingsTaken = 0) =>
        new($"forest_path-{position.X}-{position.Y}", kind, AreaId.ForestPath, position, swingsTaken);

    private static GameSession OnForestPath(Domain.Games.Game game)
    {
        var session = new GameSession(game, TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.ForestPath));
        return session;
    }

    // ---- AreaGridLayout ----

    [Fact]
    public void AreaGridLayout_maps_the_forest_path_through_its_own_grid()
    {
        var position = AreaGrid.Position(AreaId.ForestPath, 10, 38);

        Assert.Equal(ForestPathGrid.ToWorld(position), AreaGridLayout.ToWorld(AreaId.ForestPath, position));
        Assert.Equal(ForestPathGrid.CellAt(3.5f, 18.5f), AreaGridLayout.CellAt(AreaId.ForestPath, 3.5f, 18.5f));
    }

    [Theory]
    [InlineData(AreaId.Farm)]
    [InlineData(AreaId.Town)]
    [InlineData(AreaId.Lakeshore)]
    public void AreaGridLayout_maps_every_other_area_through_the_farm_grid(AreaId area)
    {
        var position = new GridPosition(40, 5);

        Assert.Equal(FarmGridLayout.ToWorld(position), AreaGridLayout.ToWorld(area, position));
        Assert.Equal(FarmGridLayout.CellAt(1.2f, 3.4f), AreaGridLayout.CellAt(area, 1.2f, 3.4f));
    }

    // ---- UseTarget ----

    [Fact]
    public void UseTarget_on_the_farm_is_the_plot_tile_whatever_is_held()
    {
        Assert.Equal(new GridPosition(5, 6), UseTarget.InFront(AreaId.Farm, 5, 6, HeldItem.Of(ToolId.Hoe)));
        Assert.Equal(new GridPosition(5, 6), UseTarget.InFront(AreaId.Farm, 5, 6, null));
        Assert.Null(UseTarget.InFront(AreaId.Farm, 5, 36, HeldItem.Of(ToolId.Axe)));
    }

    [Theory]
    [InlineData(ToolId.Axe)]
    [InlineData(ToolId.Pickaxe)]
    public void UseTarget_on_the_forest_path_is_its_tile_while_the_axe_or_pickaxe_is_held(ToolId tool)
    {
        var target = UseTarget.InFront(AreaId.ForestPath, 10, 38, HeldItem.Of(tool));

        Assert.Equal(AreaGrid.Position(AreaId.ForestPath, 10, 38), target);
    }

    [Fact]
    public void UseTarget_on_the_forest_path_is_none_for_other_items_or_off_its_grid()
    {
        Assert.Null(UseTarget.InFront(AreaId.ForestPath, 10, 12, HeldItem.Of(ToolId.Hoe)));
        Assert.Null(UseTarget.InFront(AreaId.ForestPath, 10, 12, HeldItem.Of(ToolId.WateringCan)));
        Assert.Null(UseTarget.InFront(AreaId.ForestPath, 10, 12, HeldItem.SeedOf(CropTypeId.Emberroot)));
        Assert.Null(UseTarget.InFront(AreaId.ForestPath, 10, 12, null));
        Assert.Null(UseTarget.InFront(AreaId.ForestPath, 14, 12, HeldItem.Of(ToolId.Axe)));
        Assert.Null(UseTarget.InFront(AreaId.ForestPath, 10, 40, HeldItem.Of(ToolId.Axe)));
    }

    [Theory]
    [InlineData(AreaId.Town)]
    [InlineData(AreaId.Lakeshore)]
    public void UseTarget_in_town_and_at_the_lakeshore_is_none(AreaId area)
    {
        Assert.Null(UseTarget.InFront(area, 5, 6, HeldItem.Of(ToolId.Axe)));
    }

    // ---- FeetTile ----

    [Fact]
    public void FeetTile_in_town_clamps_to_the_farm_sized_grid()
    {
        Assert.Equal(new GridPosition(71, 35), FeetTile.Nearest(AreaId.Town, 90, 50));
    }

    // ---- GridPosition and DroppedItem ----

    [Fact]
    public void A_forest_path_position_deconstructs_and_prints_its_coordinates()
    {
        var (x, y) = AreaGrid.Position(AreaId.ForestPath, 4, 39);

        Assert.Equal((4, 39), (x, y));
        Assert.Contains("39", AreaGrid.Position(AreaId.ForestPath, 4, 39).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_dropped_item_off_its_areas_grid_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DroppedItem(AreaId.ForestPath, null, new GridPosition(20, 5), ItemRef.Material(MaterialId.Wood), 1));
    }

    [Fact]
    public void A_dropped_item_on_the_forest_paths_south_rows_is_kept()
    {
        var position = AreaGrid.Position(AreaId.ForestPath, 2, 39);

        var dropped = new DroppedItem(AreaId.ForestPath, null, position, ItemRef.Material(MaterialId.Wood), 1);

        Assert.Equal(position, dropped.Position);
    }

    [Fact]
    public void The_farm_plot_refuses_a_forest_path_position_beyond_its_rows()
    {
        var plot = new FarmPlot();

        Assert.Throws<ArgumentOutOfRangeException>(() => plot[AreaGrid.Position(AreaId.ForestPath, 3, 38)]);
        Assert.Equal(new GridPosition(3, 35), plot[AreaGrid.Position(AreaId.ForestPath, 3, 35)].Position);
    }

    // ---- GameSession on the forest path ----

    [Fact]
    public void PlantingAt_gives_the_farm_planting_on_the_farm_and_none_off_it()
    {
        var ripe = TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot));
        var session = new GameSession(TestGames.WithBackpack(Tools(), tiles: ripe), TestGames.Shipped);

        Assert.Equal(CropTypeId.Emberroot, session.PlantingAt(Target)?.CropTypeId);
        Assert.Null(session.PlantingAt(null));

        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.ForestPath));
        Assert.Null(session.PlantingAt(Target));
        Assert.Null(session.PlantingAt(AreaGrid.Position(AreaId.ForestPath, 3, 38)));
    }

    [Fact]
    public void On_the_forest_path_a_ripe_farm_planting_on_the_same_tile_is_not_harvested()
    {
        var ripe = TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot));
        var game = NodeGames.WithNodes(
            TestGames.WithBackpack(Tools(), tiles: ripe), ForestNode(Target, NodeKind.Tree));
        var session = OnForestPath(game);

        Assert.Equal(new UsePreview(UseActionKind.Chop, null), session.PreviewUse(Target, AxePosition));
        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(Target, 0));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Harvest(Target));
        Assert.NotNull(session.Game.FarmPlot[Target].Planting);
    }

    [Fact]
    public void Clearing_a_forest_path_node_raises_NodesChanged_but_no_farm_TileChanged()
    {
        var session = OnForestPath(
            NodeGames.WithNodes(TestGames.WithBackpack(Tools()), ForestNode(Target, NodeKind.Tree, swingsTaken: 3)));
        var events = new List<string>();
        session.TileChanged += _ => events.Add("TileChanged");
        session.NodesChanged += () => events.Add("NodesChanged");

        Assert.Equal(ActionResult.Ok, session.Use(Target, AxePosition));

        Assert.Equal(["NodesChanged"], events);
    }

    [Fact]
    public void A_swing_that_leaves_a_forest_path_node_standing_raises_NodeSwung_for_its_tile()
    {
        var session = OnForestPath(
            NodeGames.WithNodes(TestGames.WithBackpack(Tools()), ForestNode(Target, NodeKind.Rock)));
        var swung = new List<GridPosition>();
        session.NodeSwung += swung.Add;

        Assert.Equal(ActionResult.Ok, session.Use(Target, PickaxePosition));

        Assert.Equal([Target], swung);
    }

    [Fact]
    public void A_forest_path_yield_with_no_feet_drops_on_the_nodes_tile_in_the_forest_path()
    {
        var full = TestGames.FullBackpack();
        full.RemoveFrom(3, 99);
        Assert.True(full.AddTool(ToolId.Pickaxe, ToolTier.Basic));
        var south = AreaGrid.Position(AreaId.ForestPath, 10, 36);
        var session = OnForestPath(
            NodeGames.WithNodes(TestGames.WithBackpack(full), ForestNode(south, NodeKind.Rock, swingsTaken: 3)));

        Assert.Equal(ActionResult.Ok, session.Use(south, PickaxePosition));

        var dropped = Assert.Single(session.Game.DroppedItems);
        Assert.Equal((AreaId.ForestPath, south), (dropped.Area, dropped.Position));
    }

    [Theory]
    [InlineData(AreaId.Town)]
    [InlineData(AreaId.Lakeshore)]
    public void Swing_in_town_and_at_the_lakeshore_has_no_target(AreaId area)
    {
        var session = new GameSession(TestGames.WithBackpack(Tools()), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(area));

        Assert.False(session.HasNodeGrid);
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Swing(Target, ToolId.Axe));
    }

    [Fact]
    public void The_farm_and_the_forest_path_have_node_grids()
    {
        var session = new GameSession(TestGames.WithBackpack(Tools()), TestGames.Shipped);
        Assert.True(session.HasNodeGrid);

        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.ForestPath));

        Assert.True(session.HasNodeGrid);
        Assert.False(session.IsOnFarmPlot);
    }

    // ---- Regrowth on the forest path's own rows ----

    [Fact]
    public void A_cleared_forest_path_node_beyond_the_farms_rows_waits_then_grows_back()
    {
        // The 2-Night wait itself is NodeRegrowthTests'; this checks a spawn point on rows 36–39, off the farm's grid.
        var position = AreaGrid.Position(AreaId.ForestPath, 10, 36);
        var spawn = new NodeSpawn(
            "forest_path-10-36", AreaId.ForestPath, null, position, NodeKind.Rock, AtStart: true, Respawns: true);
        var session = OnForestPath(NodeGames.WithNodes(TestGames.WithBackpack(Tools()), ResourceNode.FromSpawn(spawn)));
        for (var swing = 1; swing <= 4; swing++)
        {
            Assert.Equal(ActionResult.Ok, session.Use(position, PickaxePosition, position));
        }

        Assert.Null(session.Game.NodeAt(AreaId.ForestPath, position));

        session.Game.RegrowNodes([spawn], nights: 2);
        Assert.Null(session.Game.NodeAt(AreaId.ForestPath, position));

        session.Game.RegrowNodes([spawn], nights: 1);
        Assert.NotNull(session.Game.NodeAt(AreaId.ForestPath, position));
        Assert.Empty(session.Game.NodeRegrowths);
    }
}
