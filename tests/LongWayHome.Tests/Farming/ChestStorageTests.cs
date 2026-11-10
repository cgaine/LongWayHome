using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using static LongWayHome.Tests.Farming.PlacedGames;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Farming;

/// <summary>
/// #164: moving whole stacks between the Backpack and a placed chest's 36 slots (BR-F2). Stacks top up same-item slots
/// below 99, then fill the lowest free slots (BR-K2, BR-K3); what doesn't fit stays where it was, and moving nothing is
/// BackpackFull. Tools never enter a chest (BR-K6). Both moves are free.
/// </summary>
public class ChestStorageTests
{
    private static readonly GridPosition ChestTile = new(10, 20);

    private static PlacedObject ChestHere(GameState game) => game.FarmPlot.PlacedAt(ChestTile)!;

    private static Backpack WoodInSlot2(int quantity) => TestGames.RestoredBackpack(new BackpackSlot(2, Wood, quantity, null));

    /// <summary>36 chest slots of 99 stone, except the given ones.</summary>
    private static BackpackSlot[] StoneEverywhereBut(params BackpackSlot[] others)
    {
        var given = others.ToDictionary(slot => slot.Index);
        return Enumerable.Range(0, ChestSlotCount)
            .Select(index => given.TryGetValue(index, out var slot) ? slot : new BackpackSlot(index, Stone, 99, null))
            .ToArray();
    }

    // ---- AC4: storing and taking back ----

    [Fact]
    public void AC4_storing_a_stack_of_40_wood_moves_it_to_chest_slot_0_and_empties_its_backpack_slot()
    {
        var game = With(WoodInSlot2(40), [ChestAt(ChestTile)]);

        Assert.Equal(ActionResult.Ok, game.StoreInChest(ChestTile, 2));

        var slot0 = ChestHere(game).Slots[0];
        Assert.Equal(Wood, slot0.Item);
        Assert.Equal(40, slot0.Quantity);
        Assert.Null(game.Player.Backpack.Slots[2].Item);
        Assert.Equal(0, game.Player.Backpack.Quantity(Wood));
    }

    [Fact]
    public void AC4_taking_the_wood_back_returns_it_to_the_backpack_and_empties_the_chest_slot()
    {
        var game = With(TestGames.ToolsOnly(), [ChestAt(ChestTile, new BackpackSlot(0, Wood, 40, null))]);

        Assert.Equal(ActionResult.Ok, game.TakeFromChest(ChestTile, 0));

        Assert.Equal(Wood, game.Player.Backpack.Slots[2].Item); // the lowest free Slot
        Assert.Equal(40, game.Player.Backpack.Slots[2].Quantity);
        Assert.Null(ChestHere(game).Slots[0].Item);
        Assert.Equal(0, ChestHere(game).Slots[0].Quantity);
    }

    [Fact]
    public void AC4_storing_then_taking_back_round_trips_and_costs_no_energy()
    {
        var game = With(WoodInSlot2(40), [ChestAt(ChestTile)]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Ok, game.StoreInChest(ChestTile, 2));
        Assert.Equal(ActionResult.Ok, game.TakeFromChest(ChestTile, 0));

        Assert.Equal(before, Snapshot(game));
        Assert.Equal(150, game.Player.Energy);
    }

    [Theory]
    [InlineData(0)] // the hoe
    [InlineData(1)] // the watering can
    public void AC4_storing_a_tool_is_NotOffered_and_changes_nothing(int toolSlot)
    {
        var game = With(TestGames.ToolsOnly(), [ChestAt(ChestTile)]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), game.StoreInChest(ChestTile, toolSlot));

        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_K6_a_stored_axe_is_refused_and_keeps_its_slot_and_tier()
    {
        var backpack = TestGames.RestoredBackpack(new BackpackSlot(5, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Rosk));
        var game = With(backpack, [ChestAt(ChestTile)]);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), game.StoreInChest(ChestTile, 5));

        Assert.Equal(ToolTier.Rosk, game.Player.Backpack.TierOf(ToolId.Axe));
        Assert.Equal(ItemRef.Tool(ToolId.Axe), game.Player.Backpack.Slots[5].Item);
        Assert.All(ChestHere(game).Slots, slot => Assert.Null(slot.Item));
    }

    // ---- AC5, BR-K2: what doesn't fit stays ----

    [Fact]
    public void AC5_storing_more_than_fits_leaves_the_remainder_in_its_backpack_slot()
    {
        // 35 slots of 99 stone and one of 80 wood: 19 of the 40 wood fit.
        var game = With(WoodInSlot2(40), [ChestAt(ChestTile, StoneEverywhereBut(new BackpackSlot(35, Wood, 80, null)))]);

        Assert.Equal(ActionResult.Ok, game.StoreInChest(ChestTile, 2));

        Assert.Equal(99, ChestHere(game).Slots[35].Quantity);
        Assert.Equal(Wood, game.Player.Backpack.Slots[2].Item);
        Assert.Equal(21, game.Player.Backpack.Slots[2].Quantity);
    }

    [Fact]
    public void AC5_a_chest_slot_never_holds_over_99()
    {
        var game = With(WoodInSlot2(99), [ChestAt(ChestTile, new BackpackSlot(0, Wood, 70, null))]);

        Assert.Equal(ActionResult.Ok, game.StoreInChest(ChestTile, 2));

        Assert.All(ChestHere(game).Slots, slot => Assert.InRange(slot.Quantity, 0, 99));
        Assert.Equal(99, ChestHere(game).Slots[0].Quantity);
        Assert.Equal(70, ChestHere(game).Slots[1].Quantity);
        Assert.Equal(169, QuantityIn(ChestHere(game), Wood));
        Assert.Null(game.Player.Backpack.Slots[2].Item);
    }

    [Fact]
    public void BR_K2_storing_tops_up_a_same_item_chest_slot_below_99_before_the_lowest_free_slot()
    {
        var game = With(WoodInSlot2(20), [ChestAt(ChestTile, new BackpackSlot(3, Wood, 90, null))]);

        Assert.Equal(ActionResult.Ok, game.StoreInChest(ChestTile, 2));

        Assert.Equal(99, ChestHere(game).Slots[3].Quantity);
        Assert.Equal(Wood, ChestHere(game).Slots[0].Item);
        Assert.Equal(11, ChestHere(game).Slots[0].Quantity);
    }

    [Fact]
    public void AC5_storing_into_a_chest_with_no_room_moves_nothing_and_is_BackpackFull()
    {
        var game = With(WoodInSlot2(40), [ChestAt(ChestTile, StoneEverywhereBut())]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), game.StoreInChest(ChestTile, 2));

        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC5_taking_with_a_full_backpack_is_BackpackFull_and_the_stack_stays_in_the_chest()
    {
        var game = With(TestGames.FullBackpack(), [ChestAt(ChestTile, new BackpackSlot(0, Wood, 40, null))]);
        var before = Snapshot(game);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), game.TakeFromChest(ChestTile, 0));

        Assert.Equal(before, Snapshot(game));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_K2_taking_more_than_fits_leaves_the_remainder_in_the_chest_slot()
    {
        // Every free Slot full of filler but one of 90 wood: 9 of the 40 fit.
        var game = With(TestGames.FullBackpack((Wood, 90)), [ChestAt(ChestTile, new BackpackSlot(0, Wood, 40, null))]);

        Assert.Equal(ActionResult.Ok, game.TakeFromChest(ChestTile, 0));

        Assert.Equal(99, game.Player.Backpack.Quantity(Wood));
        Assert.Equal(31, ChestHere(game).Slots[0].Quantity);
    }

    [Fact]
    public void BR_K2_storing_a_placeable_stacks_it_like_a_material()
    {
        var game = With(
            TestGames.RestoredBackpack(new BackpackSlot(2, Fence, 7, null)),
            [ChestAt(ChestTile, new BackpackSlot(4, Fence, 3, null))]);

        Assert.Equal(ActionResult.Ok, game.StoreInChest(ChestTile, 2));

        Assert.Equal(10, ChestHere(game).Slots[4].Quantity);
        Assert.Null(ChestHere(game).Slots[0].Item);
    }
}
