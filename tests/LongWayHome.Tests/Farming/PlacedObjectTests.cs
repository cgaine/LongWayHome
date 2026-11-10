using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using static LongWayHome.Tests.Farming.PlacedGames;

namespace LongWayHome.Tests.Farming;

/// <summary>
/// #164: placing a fence or chest from a Backpack Slot on a free, untilled farm Tile (BR-F1), picking it back up
/// (BR-F2), the Placed object blocking tilling (BR-T2) and node regrowth (BR-N4), and placeables never being sold or
/// shipped (BR-L3, BR-S1).
/// </summary>
public class PlacedObjectTests
{
    private const int ToolEnergyCost = 2;

    private static readonly GridPosition Target = new(10, 20);

    private static readonly GridPosition Neighbour = new(11, 20);

    private static Backpack ChestInSlot3(int quantity = 2) => TestGames.RestoredBackpack(new BackpackSlot(3, Chest, quantity, null));

    private static Backpack FenceInSlot3(int quantity = 2) => TestGames.RestoredBackpack(new BackpackSlot(3, Fence, quantity, null));

    // ---- AC1, BR-F1: placing ----

    [Fact]
    public void AC1_placing_the_chest_from_slot_3_on_free_grass_puts_a_chest_with_36_empty_slots_there()
    {
        var game = Plain(ChestInSlot3());

        var result = game.Place(Target, 3);

        Assert.Equal(ActionResult.Ok, result);
        var placed = game.FarmPlot.PlacedAt(Target);
        Assert.NotNull(placed);
        Assert.Equal(PlaceableId.Chest, placed.Placeable);
        Assert.Equal(Target, placed.Position);
        Assert.Equal(Enumerable.Range(0, 36), placed.Slots.Select(slot => slot.Index));
        Assert.All(placed.Slots, slot =>
        {
            Assert.Null(slot.Item);
            Assert.Equal(0, slot.Quantity);
            Assert.Null(slot.ToolTier);
        });
    }

    [Fact]
    public void AC1_placing_a_chest_removes_one_chest_from_slot_3()
    {
        var game = Plain(ChestInSlot3(quantity: 2));

        Assert.Equal(ActionResult.Ok, game.Place(Target, 3));

        var slot = game.Player.Backpack.Slots[3];
        Assert.Equal(Chest, slot.Item);
        Assert.Equal(1, slot.Quantity);
    }

    [Fact]
    public void BR_F1_placing_the_last_chest_empties_its_slot_and_costs_no_energy()
    {
        var game = Plain(ChestInSlot3(quantity: 1));

        Assert.Equal(ActionResult.Ok, game.Place(Target, 3));

        Assert.Null(game.Player.Backpack.Slots[3].Item);
        Assert.Equal(0, game.Player.Backpack.Slots[3].Quantity);
        Assert.Equal(150, game.Player.Energy);
    }

    [Fact]
    public void BR_F1_a_placed_fence_stands_on_its_tile_with_no_slots()
    {
        var game = Plain(FenceInSlot3());

        Assert.Equal(ActionResult.Ok, game.Place(Target, 3));

        var placed = game.FarmPlot.PlacedAt(Target);
        Assert.NotNull(placed);
        Assert.Equal(PlaceableId.Fence, placed.Placeable);
        Assert.Empty(placed.Slots);
        Assert.Equal(1, game.Player.Backpack.Slots[3].Quantity);
        Assert.Single(game.FarmPlot.PlacedObjects);
    }

    [Fact]
    public void BR_F1_the_tile_stays_untilled_grass_and_its_neighbour_holds_nothing()
    {
        var game = Plain(FenceInSlot3());

        Assert.Equal(ActionResult.Ok, game.Place(Target, 3));

        Assert.Equal(TileState.Grass, game.FarmPlot[Target].State);
        Assert.Null(game.FarmPlot.PlacedAt(Neighbour));
    }

    [Fact]
    public void BR_F1_placing_off_the_farm_grid_is_NoTarget()
    {
        // The forest path's 14 x 40 grid has rows the 72 x 36 farm doesn't.
        Assert.True(AreaGrid.TryCreate(AreaId.ForestPath, 5, 39, out var offFarm));
        var game = Plain(ChestInSlot3());

        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), game.Place(offFarm, 3));
        Assert.Equal(2, game.Player.Backpack.Slots[3].Quantity);
    }

    // ---- AC2: where a placeable can't go ----

    [Fact]
    public void AC2_placing_on_a_tilled_tile_is_AlreadyTilled_and_changes_nothing()
    {
        var game = Plain(ChestInSlot3(), tiles: [new Tile(Target, TileState.Tilled, false, null)]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), game.Place(Target, 3));

        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC2_placing_on_a_node_is_NodeInTheWay_and_changes_nothing()
    {
        var game = Plain(ChestInSlot3(), nodes: [NodeGames.Node(Target.X, Target.Y, NodeKind.Rock)]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.NodeInTheWay), game.Place(Target, 3));

        Assert.Equal(before, Snapshot(game));
        Assert.NotNull(game.NodeAt(AreaId.Farm, Target));
    }

    [Theory]
    [InlineData(PlaceableId.Fence)]
    [InlineData(PlaceableId.Chest)]
    public void AC2_placing_on_a_placed_object_is_TileOccupied_and_changes_nothing(PlaceableId already)
    {
        PlacedObject standing = already == PlaceableId.Fence ? FenceAt(Target) : ChestAt(Target);
        var game = With(ChestInSlot3(), [standing]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), game.Place(Target, 3));

        Assert.Equal(before, Snapshot(game));
        Assert.Equal(already, game.FarmPlot.PlacedAt(Target)!.Placeable);
    }

    [Fact]
    public void AC2_placing_on_a_planting_is_refused_and_changes_nothing()
    {
        // A Planting only ever sits on tilled soil (BR-T3), so the Tile is tilled as well as occupied: the spec's
        // AlreadyTilled-before-TileOccupied order and AC2's "TileOccupied on a Planting" both refuse it.
        var game = Plain(
            ChestInSlot3(),
            tiles: [TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 1))]);
        var before = Snapshot(game);

        var result = game.Place(Target, 3);

        Assert.Contains(result.Rejection, new Rejection?[] { Rejection.AlreadyTilled, Rejection.TileOccupied });
        Assert.Equal(before, Snapshot(game));
    }

    [Theory]
    [InlineData(0)] // the hoe
    [InlineData(2)] // 40 wood
    [InlineData(4)] // 6 emberroot seeds
    [InlineData(5)] // empty
    public void AC2_placing_from_a_slot_that_holds_no_placeable_is_NothingToDo_and_changes_nothing(int slotIndex)
    {
        var backpack = TestGames.RestoredBackpack(
            new BackpackSlot(2, Wood, 40, null),
            new BackpackSlot(3, Chest, 1, null),
            new BackpackSlot(4, ItemRef.Seed(CropTypeId.Emberroot), 6, null));
        var game = Plain(backpack);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.Place(Target, slotIndex));

        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC3, BR-T2: a placed object blocks tilling ----

    [Fact]
    public void AC3_tilling_a_tile_holding_a_fence_is_refused_with_TileOccupied()
    {
        var game = With(TestGames.ToolsOnly(), [FenceAt(Target)]);

        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), game.Till(Target, ToolEnergyCost));
    }

    [Fact]
    public void AC3_a_refused_till_on_a_fence_costs_no_energy_and_leaves_the_tile_grass()
    {
        var game = With(TestGames.ToolsOnly(), [FenceAt(Target)]);

        game.Till(Target, ToolEnergyCost);

        Assert.Equal(150, game.Player.Energy);
        Assert.Equal(TileState.Grass, game.FarmPlot[Target].State);
        Assert.NotNull(game.FarmPlot.PlacedAt(Target));
    }

    [Fact]
    public void BR_T2_till_rejection_on_a_chests_tile_is_TileOccupied()
    {
        var game = With(TestGames.ToolsOnly(), [ChestAt(Target)]);

        Assert.Equal(Rejection.TileOccupied, game.TillRejection(Target, ToolEnergyCost));
    }

    [Fact]
    public void BR_T2_a_grass_tile_next_to_a_fence_still_tills()
    {
        var game = With(TestGames.ToolsOnly(), [FenceAt(Target)]);

        Assert.Equal(ActionResult.Ok, game.Till(Neighbour, ToolEnergyCost));
        Assert.Equal(TileState.Tilled, game.FarmPlot[Neighbour].State);
    }

    // ---- AC6, BR-F2: picking up ----

    [Fact]
    public void AC6_picking_up_a_fence_adds_it_to_the_backpack_and_frees_the_tile()
    {
        var game = With(TestGames.ToolsOnly(), [FenceAt(Target)]);

        Assert.Equal(ActionResult.Ok, game.PickUpPlaced(Target));

        Assert.Null(game.FarmPlot.PlacedAt(Target));
        Assert.Empty(game.FarmPlot.PlacedObjects);
        Assert.Equal(1, game.Player.Backpack.Quantity(Fence));
        Assert.Equal(Fence, game.Player.Backpack.Slots[2].Item); // the lowest free Slot
        Assert.Equal(150, game.Player.Energy);
    }

    [Fact]
    public void AC6_picking_up_an_empty_chest_adds_it_to_the_backpack_and_frees_the_tile()
    {
        var game = With(TestGames.ToolsOnly(), [ChestAt(Target)]);

        Assert.Equal(ActionResult.Ok, game.PickUpPlaced(Target));

        Assert.Null(game.FarmPlot.PlacedAt(Target));
        Assert.Equal(1, game.Player.Backpack.Quantity(Chest));
    }

    [Fact]
    public void AC6_a_freed_tile_can_be_placed_on_again()
    {
        var game = With(TestGames.ToolsOnly(), [FenceAt(Target)]);
        Assert.Equal(ActionResult.Ok, game.PickUpPlaced(Target));

        Assert.Equal(ActionResult.Ok, game.Place(Target, 2));

        Assert.Equal(PlaceableId.Fence, game.FarmPlot.PlacedAt(Target)!.Placeable);
    }

    [Fact]
    public void BR_F2_a_picked_up_fence_tops_up_a_matching_stack_before_a_free_slot()
    {
        var game = With(TestGames.RestoredBackpack(new BackpackSlot(4, Fence, 5, null)), [FenceAt(Target)]);

        Assert.Equal(ActionResult.Ok, game.PickUpPlaced(Target));

        Assert.Equal(6, game.Player.Backpack.Slots[4].Quantity);
        Assert.Null(game.Player.Backpack.Slots[2].Item);
    }

    [Fact]
    public void AC6_picking_up_a_chest_holding_anything_is_ChestNotEmpty_and_it_stays()
    {
        var game = With(TestGames.ToolsOnly(), [ChestAt(Target, new BackpackSlot(17, Stone, 1, null))]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.ChestNotEmpty), game.PickUpPlaced(Target));

        Assert.Equal(before, Snapshot(game));
        Assert.Equal(1, QuantityIn(game.FarmPlot.PlacedAt(Target)!, Stone));
    }

    [Theory]
    [InlineData(PlaceableId.Fence)]
    [InlineData(PlaceableId.Chest)]
    public void AC6_with_a_full_backpack_picking_up_is_BackpackFull_and_the_object_stays(PlaceableId placeable)
    {
        PlacedObject standing = placeable == PlaceableId.Fence ? FenceAt(Target) : ChestAt(Target);
        var game = With(TestGames.FullBackpack(), [standing]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), game.PickUpPlaced(Target));

        Assert.Equal(before, Snapshot(game));
        Assert.Empty(game.DroppedItems); // it never drops at the feet
    }

    [Fact]
    public void BR_F2_picking_up_where_nothing_is_placed_is_NothingToDo()
    {
        var game = With(TestGames.ToolsOnly(), [FenceAt(Neighbour)]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.PickUpPlaced(Target));

        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC7, BR-N4: a placed object blocks regrowth ----

    private static GameDate Spring(int day) => new(1, Season.Spring, day);

    private static NodeSpawn TreeSpawn(GridPosition at) =>
        new($"farm-{at.X}-{at.Y}", AreaId.Farm, null, at, NodeKind.Tree, AtStart: true, Respawns: true);

    [Fact]
    public void AC7_a_farm_spawn_point_under_a_placed_object_does_not_regrow_and_keeps_waiting()
    {
        var spawn = TreeSpawn(Target);
        var regrowth = new NodeRegrowth(spawn.Id, Spring(3));
        var game = With(TestGames.ToolsOnly(), [FenceAt(Target)], regrowths: [regrowth], date: Spring(10));

        game.RegrowNodes([spawn], 2);

        Assert.Null(game.NodeAt(AreaId.Farm, Target));
        Assert.Equal([regrowth], game.NodeRegrowths);
    }

    [Fact]
    public void AC7_once_the_object_is_picked_up_and_the_wait_is_over_the_node_regrows()
    {
        var spawn = TreeSpawn(Target);
        var game = With(
            TestGames.ToolsOnly(), [ChestAt(Target)], regrowths: [new NodeRegrowth(spawn.Id, Spring(3))], date: Spring(10));
        game.RegrowNodes([spawn], 2);
        Assert.Null(game.NodeAt(AreaId.Farm, Target));

        Assert.Equal(ActionResult.Ok, game.PickUpPlaced(Target));
        game.RegrowNodes([spawn], 2);

        Assert.NotNull(game.NodeAt(AreaId.Farm, Target));
        Assert.Empty(game.NodeRegrowths);
    }

    [Fact]
    public void BR_N4_a_placed_object_on_a_neighbouring_tile_does_not_block_regrowth()
    {
        var spawn = TreeSpawn(Target);
        var game = With(
            TestGames.ToolsOnly(), [FenceAt(Neighbour)], regrowths: [new NodeRegrowth(spawn.Id, Spring(3))], date: Spring(10));

        game.RegrowNodes([spawn], 2);

        Assert.NotNull(game.NodeAt(AreaId.Farm, Target));
    }

    // ---- AC8, BR-L3, BR-S1: placeables can't be sold or shipped ----

    private static Backpack PlaceablesAndACrop() => TestGames.RestoredBackpack(
        new BackpackSlot(2, Fence, 4, null),
        new BackpackSlot(3, ItemRef.Crop(CropTypeId.Emberroot), 2, null),
        new BackpackSlot(4, Chest, 1, null));

    private static GameSession SessionInTown(Backpack backpack)
    {
        var session = new GameSession(TestGames.WithBackpack(backpack), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        return session;
    }

    [Fact]
    public void AC8_fences_and_chests_never_appear_in_maggies_sell_list()
    {
        var session = SessionInTown(PlaceablesAndACrop());

        Assert.Equal([CropTypeId.Emberroot], session.SellableCrops().Select(row => row.CropTypeId));
    }

    [Fact]
    public void BR_L3_sell_all_to_maggie_leaves_the_fences_and_chests_in_the_backpack()
    {
        var session = SessionInTown(PlaceablesAndACrop());

        Assert.Equal(ActionResult.Ok, session.SellAllToMaggie());

        Assert.Equal(4, session.Game.Player.Backpack.Quantity(Fence));
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(Chest));
        Assert.Equal(0, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }

    [Fact]
    public void BR_L3_with_only_placeables_carried_there_is_nothing_to_sell()
    {
        var session = SessionInTown(TestGames.RestoredBackpack(new BackpackSlot(2, Chest, 3, null)));

        Assert.Empty(session.SellableCrops());
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), session.SellAllToMaggie());
        Assert.Equal(3, session.Game.Player.Backpack.Quantity(Chest));
    }

    [Fact]
    public void BR_S1_ship_all_ships_the_crops_and_leaves_the_placeables_in_the_backpack()
    {
        var game = TestGames.WithBackpack(PlaceablesAndACrop());

        Assert.Equal(ActionResult.Ok, game.ShipAll());

        Assert.Equal(4, game.Player.Backpack.Quantity(Fence));
        Assert.Equal(1, game.Player.Backpack.Quantity(Chest));
        Assert.Equal([CropTypeId.Emberroot], game.ShippingBin.Items.Select(item => item.CropTypeId));
    }

    [Fact]
    public void AC8_with_only_placeables_carried_ship_all_is_nothing_to_ship()
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(new BackpackSlot(2, Fence, 3, null)));

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToShip), game.ShipAll());

        Assert.Equal(3, game.Player.Backpack.Quantity(Fence));
        Assert.Empty(game.ShippingBin.Items);
    }
}
