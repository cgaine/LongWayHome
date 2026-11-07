using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// Dropped items (#130): <c>Game.Gain</c>, <c>Game.PickUp</c>, <c>Game.ClearDroppedItems</c> and the
/// <see cref="DroppedItem"/> invariants: BR-K2, BR-K3, BR-K4, BR-K6, BR-P2, BR-T6.
/// </summary>
public class DroppedItemTests
{
    private static readonly GridPosition Target = new(6, 3);

    private static readonly GridPosition Feet = new(2, 5);

    private static readonly CropType Emberroot = TestGames.Crop(CropTypeId.Emberroot);

    private static readonly ItemRef EmberrootCrop = ItemRef.Crop(CropTypeId.Emberroot);

    private static readonly ItemRef DusktuberCrop = ItemRef.Crop(CropTypeId.Dusktuber);

    /// <summary>A Game whose Backpack is the tools plus every free Slot full of <see cref="TestGames.Filler"/>.</summary>
    private static GameState FullGame(int energy = 150, params (ItemRef Item, int Quantity)[] items) =>
        TestGames.WithBackpack(TestGames.FullBackpack(items), energy: energy);

    private static (int Energy, string Backpack) State(GameState game) =>
        (game.Player.Energy, TestGames.Describe(game.Player.Backpack));

    // ---- AC1: a harvest with no room drops the Crop at the feet ----

    [Fact]
    public void AC1_harvest_with_no_room_drops_the_crop_at_the_feet_tile()
    {
        var game = TestGames.WithBackpack(
            TestGames.FullBackpack(), tiles: TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot), watered: true));
        var before = TestGames.Describe(game.Player.Backpack);

        var result = game.Harvest(Target, Emberroot, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Null(game.FarmPlot[Target].Planting);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.True(game.FarmPlot[Target].Watered);
        Assert.Equal(before, TestGames.Describe(game.Player.Backpack));
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal(AreaId.Farm, dropped.Area);
        Assert.Null(dropped.MineLevel);
        Assert.Equal(Feet, dropped.Position);
        Assert.Equal(EmberrootCrop, dropped.Item);
        Assert.Equal(1, dropped.Quantity);
    }

    [Fact]
    public void AC1_harvest_with_no_room_and_no_feet_drops_on_the_harvested_tile()
    {
        var game = TestGames.WithBackpack(
            TestGames.FullBackpack(), tiles: TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal(Target, dropped.Position);
    }

    // ---- AC2: what fits goes in first, only the rest drops ----

    [Fact]
    public void AC2_gain_places_what_fits_first_and_drops_the_remainder()
    {
        // Slot 2 holds 95 emberroot crops, Slots 3–10 are full of filler, Slot 11 is free: room for 4 + 99.
        var slots = new List<BackpackSlot> { new(2, EmberrootCrop, 95, null) };
        slots.AddRange(Enumerable.Range(3, 8).Select(index => new BackpackSlot(index, TestGames.Filler, 99, null)));
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack([.. slots]));

        var result = game.Gain(EmberrootCrop, 110, Feet);

        Assert.Equal(103, result.Added);
        Assert.Equal("2:Crop:Emberroot:99:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Equal("11:Crop:Emberroot:99:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[11]));
        var dropped = Assert.Single(result.Dropped);
        Assert.Equal(7, dropped.Quantity);
        Assert.Equal(Feet, dropped.Position);
        Assert.Equal(EmberrootCrop, dropped.Item);
        Assert.Same(dropped, Assert.Single(game.DroppedItems));
    }

    [Fact]
    public void AC2_gain_that_all_fits_drops_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack());

        var result = game.Gain(EmberrootCrop, 120, Feet);

        Assert.Equal(120, result.Added);
        Assert.Empty(result.Dropped);
        Assert.Empty(game.DroppedItems);
        Assert.Equal(120, game.Player.Backpack.Quantity(EmberrootCrop));
    }

    // ---- AC3: a remainder above 99 drops in chunks of at most 99, never merged ----

    [Fact]
    public void AC3_remainder_above_99_drops_in_chunks_of_at_most_99()
    {
        var game = FullGame();
        game.Gain(EmberrootCrop, 5, Feet);

        var result = game.Gain(EmberrootCrop, 150, Feet);

        Assert.Equal(0, result.Added);
        Assert.Equal([99, 51], result.Dropped.Select(item => item.Quantity));
        Assert.Equal([5, 99, 51], game.DroppedItems.Select(item => item.Quantity));
        Assert.All(game.DroppedItems, item => Assert.Equal(Feet, item.Position));
        Assert.All(game.DroppedItems, item => Assert.Equal(EmberrootCrop, item.Item));
    }

    // ---- AC5, AC6: picking up ----

    [Fact]
    public void AC5_pick_up_moves_the_item_back_and_removes_it()
    {
        var game = FullGame();
        game.Gain(EmberrootCrop, 3, Feet);
        Assert.True(game.Player.Backpack.Remove(TestGames.Filler, 99)); // frees Slot 11

        var picked = game.PickUp(Feet);

        Assert.True(picked);
        Assert.Empty(game.DroppedItems);
        Assert.Equal("11:Crop:Emberroot:3:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[11]));
    }

    [Fact]
    public void AC5_pick_up_tops_up_a_matching_stack_first()
    {
        // Slot 2 holds 98 emberroot crops: Gain tops it to 99, then drops 4.
        var game = FullGame(items: (EmberrootCrop, 98));
        game.Gain(EmberrootCrop, 5, Feet);
        Assert.True(game.Player.Backpack.Remove(EmberrootCrop, 2)); // Slot 2 back to 97
        Assert.True(game.Player.Backpack.Remove(TestGames.Filler, 99)); // frees Slot 11

        var picked = game.PickUp(Feet);

        Assert.True(picked);
        Assert.Empty(game.DroppedItems);
        Assert.Equal("2:Crop:Emberroot:99:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Equal("11:Crop:Emberroot:2:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[11]));
    }

    [Fact]
    public void AC6_partial_pick_up_reduces_the_quantity_on_the_ground()
    {
        // Slot 2 holds 80 emberroot crops: Gain of 69 tops it to 99 and drops 50.
        var game = FullGame(items: (EmberrootCrop, 80));
        game.Gain(EmberrootCrop, 69, Feet);
        Assert.True(game.Player.Backpack.Remove(EmberrootCrop, 20)); // room for 20

        var picked = game.PickUp(Feet);

        Assert.True(picked);
        Assert.Equal("2:Crop:Emberroot:99:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        var left = Assert.Single(game.DroppedItems);
        Assert.Equal(30, left.Quantity);
        Assert.Equal(Feet, left.Position);
    }

    // ---- AC7: no energy, and nothing without room ----

    [Fact]
    public void AC7_pick_up_does_nothing_without_room()
    {
        var game = FullGame(energy: 40);
        game.Gain(EmberrootCrop, 5, Feet);
        var before = State(game);

        var picked = game.PickUp(Feet);

        Assert.False(picked);
        Assert.Equal(before, State(game));
        Assert.Equal(5, Assert.Single(game.DroppedItems).Quantity);
    }

    [Fact]
    public void AC7_pick_up_on_a_tile_with_nothing_dropped_does_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack(), energy: 40);
        game.Gain(EmberrootCrop, 1, Feet); // fits: nothing on the ground
        var before = State(game);

        var picked = game.PickUp(new GridPosition(7, 7));

        Assert.False(picked);
        Assert.Equal(before, State(game));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_P2_pick_up_costs_no_energy()
    {
        var game = FullGame(energy: 40);
        game.Gain(EmberrootCrop, 5, Feet);
        Assert.True(game.Player.Backpack.Remove(TestGames.Filler, 99));

        var picked = game.PickUp(Feet);

        Assert.True(picked);
        Assert.Equal(40, game.Player.Energy);
    }

    // ---- AC13, BR-K6: tools never drop or move ----

    [Fact]
    public void AC13_gain_refuses_a_tool()
    {
        var game = FullGame();
        var before = State(game);

        Assert.Throws<ArgumentException>(() => game.Gain(ItemRef.Tool(ToolId.Hoe), 1, Feet));
        Assert.Equal(before, State(game));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void AC13_pick_up_never_moves_a_tool()
    {
        var game = FullGame();
        game.Gain(EmberrootCrop, 3, Feet);
        Assert.True(game.Player.Backpack.Remove(TestGames.Filler, 99));

        Assert.True(game.PickUp(Feet));

        Assert.Equal("0:Tool:Hoe:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[0]));
        Assert.Equal("1:Tool:WateringCan:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[1]));
        Assert.DoesNotContain(game.DroppedItems, item => item.Item.Kind == ItemKind.Tool);
    }

    [Fact]
    public void BR_K6_dropped_item_cannot_hold_a_tool()
    {
        Assert.Throws<ArgumentException>(
            () => new DroppedItem(AreaId.Farm, null, Feet, ItemRef.Tool(ToolId.WateringCan), 1));
    }

    // ---- BR-K2: 1 to 99 of one item ----

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void BR_K2_dropped_item_quantity_must_be_1_to_99(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DroppedItem(AreaId.Farm, null, Feet, EmberrootCrop, quantity));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void BR_K2_dropped_item_holds_1_to_99_of_one_item(int quantity)
    {
        var item = new DroppedItem(AreaId.Farm, null, Feet, ItemRef.Seed(CropTypeId.Dusktuber), quantity);

        Assert.Equal((AreaId.Farm, (int?)null, Feet, ItemRef.Seed(CropTypeId.Dusktuber), quantity),
            (item.Area, item.MineLevel, item.Position, item.Item, item.Quantity));
    }

    // ---- BR-K3: matching stack, then the lowest free Slot ----

    [Fact]
    public void BR_K3_gain_tops_up_matching_stack_before_free_slot()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((ItemRef.Seed(CropTypeId.Emberroot), 10)));

        var result = game.Gain(ItemRef.Seed(CropTypeId.Emberroot), 5, Feet);

        Assert.Equal(5, result.Added);
        Assert.Empty(result.Dropped);
        Assert.Equal("2:Seed:Emberroot:15:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Null(game.Player.Backpack.Slots[3].Item);
        Assert.Empty(game.DroppedItems);
    }

    // ---- BR-K4: oldest first; the Night clears them ----

    [Fact]
    public void BR_K4_pick_up_takes_oldest_first()
    {
        var game = FullGame();
        game.Gain(EmberrootCrop, 3, Feet);
        game.Gain(DusktuberCrop, 4, Feet);
        Assert.True(game.Player.Backpack.Remove(TestGames.Filler, 99)); // one free Slot

        Assert.True(game.PickUp(Feet));

        Assert.Equal("11:Crop:Emberroot:3:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[11]));
        var left = Assert.Single(game.DroppedItems);
        Assert.Equal((DusktuberCrop, 4), (left.Item, left.Quantity));
    }

    [Fact]
    public void BR_K4_pick_up_only_takes_what_lies_at_the_position()
    {
        var elsewhere = new GridPosition(9, 1);
        var game = FullGame();
        game.Gain(EmberrootCrop, 3, elsewhere);
        Assert.True(game.Player.Backpack.Remove(TestGames.Filler, 99));

        Assert.False(game.PickUp(Feet));

        Assert.Equal(elsewhere, Assert.Single(game.DroppedItems).Position);
    }

    [Fact]
    public void BR_K4_clear_dropped_items_removes_every_one()
    {
        var game = FullGame();
        game.Gain(EmberrootCrop, 150, Feet);
        game.Gain(DusktuberCrop, 2, new GridPosition(0, 0));
        Assert.Equal(3, game.DroppedItems.Count);

        game.ClearDroppedItems();

        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_K4_a_new_game_has_no_dropped_items()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack());

        Assert.Empty(game.DroppedItems);
    }
}
