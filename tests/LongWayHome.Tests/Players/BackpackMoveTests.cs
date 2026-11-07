using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Players;

/// <summary>Rearranging Backpack Slots (#131): <see cref="Backpack.Move"/> moves, merges or swaps (BR-K2, BR-K6).</summary>
public class BackpackMoveTests
{
    private static readonly ItemRef EmberrootSeed = ItemRef.Seed(CropTypeId.Emberroot);

    private static readonly ItemRef DusktuberSeed = ItemRef.Seed(CropTypeId.Dusktuber);

    private static readonly ItemRef EmberrootCrop = ItemRef.Crop(CropTypeId.Emberroot);

    private static readonly ItemRef MoonmelonCrop = ItemRef.Crop(CropTypeId.Moonmelon);

    private static readonly ItemRef HoeItem = ItemRef.Tool(ToolId.Hoe);

    private static readonly ItemRef CanItem = ItemRef.Tool(ToolId.WateringCan);

    private static BackpackSlot Stack(int index, ItemRef item, int quantity) => new(index, item, quantity, null);

    private static string Slot(Backpack backpack, int index) => TestGames.DescribeSlot(backpack.Slots[index]);

    // ---- AC4: onto an empty Slot ----

    [Fact]
    public void AC4_Moving_the_hoe_onto_an_empty_slot_moves_it_with_its_tier()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.SetTier(ToolId.Hoe, ToolTier.Rosk);

        var moved = backpack.Move(0, 7);

        Assert.True(moved);
        Assert.Equal("0:-:-:0:-", Slot(backpack, 0));
        Assert.Equal("7:Tool:Hoe:1:Rosk", Slot(backpack, 7));
        Assert.Equal(ToolTier.Rosk, backpack.TierOf(ToolId.Hoe));
    }

    [Fact]
    public void AC4_Moving_a_stack_onto_an_empty_slot_moves_the_whole_stack()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 42));

        var moved = backpack.Move(2, 11);

        Assert.True(moved);
        Assert.Equal("2:-:-:0:-", Slot(backpack, 2));
        Assert.Equal("11:Seed:Emberroot:42:-", Slot(backpack, 11));
    }

    [Fact]
    public void AC4_A_move_keeps_every_slots_index()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 42));

        Assert.True(backpack.Move(2, 9));
        Assert.True(backpack.Move(0, 2));

        Assert.Equal(Enumerable.Range(0, backpack.Capacity), backpack.Slots.Select(slot => slot.Index));
    }

    // ---- AC5: onto the same non-tool item ----

    [Fact]
    public void AC5_Merging_60_onto_60_of_the_same_seed_gives_99_in_the_target_and_21_in_the_source()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 60), Stack(5, EmberrootSeed, 60));

        var moved = backpack.Move(2, 5);

        Assert.True(moved);
        Assert.Equal("5:Seed:Emberroot:99:-", Slot(backpack, 5));
        Assert.Equal("2:Seed:Emberroot:21:-", Slot(backpack, 2));
        Assert.Equal(120, backpack.Quantity(EmberrootSeed));
    }

    [Fact]
    public void AC5_Merging_30_onto_40_of_the_same_crop_gives_70_in_the_target_and_empties_the_source()
    {
        var backpack = TestGames.RestoredBackpack(Stack(3, EmberrootCrop, 30), Stack(8, EmberrootCrop, 40));

        var moved = backpack.Move(3, 8);

        Assert.True(moved);
        Assert.Equal("8:Crop:Emberroot:70:-", Slot(backpack, 8));
        Assert.Equal("3:-:-:0:-", Slot(backpack, 3));
    }

    [Fact]
    public void AC5_Merging_exactly_to_99_empties_the_source()
    {
        var backpack = TestGames.RestoredBackpack(Stack(3, DusktuberSeed, 49), Stack(4, DusktuberSeed, 50));

        var moved = backpack.Move(3, 4);

        Assert.True(moved);
        Assert.Equal("4:Seed:Dusktuber:99:-", Slot(backpack, 4));
        Assert.Equal("3:-:-:0:-", Slot(backpack, 3));
    }

    // ---- AC6: onto a different item or a tool ----

    [Fact]
    public void AC6_Moving_a_tool_onto_a_seed_slot_swaps_them()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10));

        var moved = backpack.Move(0, 2);

        Assert.True(moved);
        Assert.Equal("0:Seed:Emberroot:10:-", Slot(backpack, 0));
        Assert.Equal("2:Tool:Hoe:1:Basic", Slot(backpack, 2));
    }

    [Fact]
    public void AC6_Moving_a_seed_onto_a_tool_swaps_them()
    {
        var backpack = TestGames.RestoredBackpack(Stack(4, DusktuberSeed, 5));

        var moved = backpack.Move(4, 1);

        Assert.True(moved);
        Assert.Equal("1:Seed:Dusktuber:5:-", Slot(backpack, 1));
        Assert.Equal("4:Tool:WateringCan:1:Basic", Slot(backpack, 4));
    }

    [Fact]
    public void AC6_Moving_a_seed_onto_a_different_crop_swaps_them()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10), Stack(6, MoonmelonCrop, 3));

        var moved = backpack.Move(2, 6);

        Assert.True(moved);
        Assert.Equal("2:Crop:Moonmelon:3:-", Slot(backpack, 2));
        Assert.Equal("6:Seed:Emberroot:10:-", Slot(backpack, 6));
    }

    [Fact]
    public void AC6_The_seed_and_crop_of_one_crop_type_are_different_items_and_swap()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10), Stack(3, EmberrootCrop, 7));

        var moved = backpack.Move(2, 3);

        Assert.True(moved);
        Assert.Equal("2:Crop:Emberroot:7:-", Slot(backpack, 2));
        Assert.Equal("3:Seed:Emberroot:10:-", Slot(backpack, 3));
    }

    [Fact]
    public void AC6_Two_tools_of_different_tiers_swap_and_keep_their_tiers()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.SetTier(ToolId.WateringCan, ToolTier.Groth);

        var moved = backpack.Move(0, 1);

        Assert.True(moved);
        Assert.Equal("0:Tool:WateringCan:1:Groth", Slot(backpack, 0));
        Assert.Equal("1:Tool:Hoe:1:Basic", Slot(backpack, 1));
        Assert.Equal(ToolTier.Basic, backpack.TierOf(ToolId.Hoe));
        Assert.Equal(ToolTier.Groth, backpack.TierOf(ToolId.WateringCan));
    }

    // ---- AC7: nothing to move ----

    [Fact]
    public void AC7_Moving_a_slot_onto_itself_returns_false_and_changes_nothing()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10));
        var before = TestGames.Describe(backpack);

        var moved = backpack.Move(2, 2);

        Assert.False(moved);
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void AC7_Moving_a_tool_onto_itself_returns_false_and_changes_nothing()
    {
        var backpack = TestGames.ToolsOnly();
        var before = TestGames.Describe(backpack);

        Assert.False(backpack.Move(0, 0));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void AC7_Moving_from_an_empty_slot_returns_false_and_changes_nothing()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10));
        var before = TestGames.Describe(backpack);

        Assert.False(backpack.Move(5, 2));
        Assert.False(backpack.Move(5, 6));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void AC7_Merging_onto_a_full_99_stack_of_the_same_item_returns_false_and_changes_nothing()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 5), Stack(3, EmberrootSeed, 99));
        var before = TestGames.Describe(backpack);

        var moved = backpack.Move(2, 3);

        Assert.False(moved);
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    // ---- AC8: any sequence of moves keeps the tools, the tiers and the totals ----

    [Theory]
    [InlineData(12, 131)]
    [InlineData(12, 7)]
    [InlineData(24, 2026)]
    [InlineData(36, 99)]
    public void AC8_Any_sequence_of_moves_keeps_one_hoe_one_can_their_tiers_and_every_items_total(int capacity, int seed)
    {
        var backpack = TestGames.RestoredBackpack(
            Stack(2, EmberrootSeed, 60),
            Stack(3, EmberrootSeed, 60),
            Stack(4, DusktuberSeed, 99),
            Stack(5, DusktuberSeed, 7),
            Stack(6, EmberrootCrop, 30),
            Stack(7, EmberrootCrop, 45),
            Stack(9, MoonmelonCrop, 1));
        backpack.SetTier(ToolId.WateringCan, ToolTier.Rosk);
        if (capacity > backpack.Capacity)
        {
            backpack.RaiseCapacity(capacity);
        }

        ItemRef[] items = [EmberrootSeed, DusktuberSeed, EmberrootCrop, MoonmelonCrop];
        var totals = items.ToDictionary(item => item, backpack.Quantity);
        var random = new Random(seed);
        var movedCount = 0;

        for (var step = 0; step < 1000; step++)
        {
            var from = random.Next(backpack.Capacity);
            var to = random.Next(backpack.Capacity);

            if (backpack.Move(from, to))
            {
                movedCount++;
            }

            var context = $"after move {step} ({from} -> {to})";
            Assert.True(backpack.Slots.Count(slot => slot.Item == HoeItem) == 1, $"not exactly one hoe {context}");
            Assert.True(backpack.Slots.Count(slot => slot.Item == CanItem) == 1, $"not exactly one can {context}");
            Assert.Equal(ToolTier.Basic, backpack.TierOf(ToolId.Hoe));
            Assert.Equal(ToolTier.Rosk, backpack.TierOf(ToolId.WateringCan));
            foreach (var item in items)
            {
                Assert.True(totals[item] == backpack.Quantity(item), $"{item} total changed {context}");
            }

            Assert.Equal(capacity, backpack.Capacity);
            for (var index = 0; index < backpack.Capacity; index++)
            {
                Assert.Equal(index, backpack.Slots[index].Index);
                AssertValidSlot(backpack.Slots[index], context);
            }
        }

        // The sequence really rearranged the Backpack, so the checks above are not vacuous.
        Assert.True(movedCount > 50, $"only {movedCount} of 1000 moves moved anything");
    }

    // ---- BR-K2: indices and stack size ----

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(12, 2)]
    [InlineData(2, -1)]
    [InlineData(2, 12)]
    public void BR_K2_An_index_outside_the_backpack_throws_and_changes_nothing(int from, int to)
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10));
        var before = TestGames.Describe(backpack);

        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.Move(from, to));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void BR_K2_Index_Capacity_throws_on_a_24_slot_backpack()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.RaiseCapacity(24);

        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.Move(0, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.Move(24, 0));
    }

    [Fact]
    public void BR_K2_After_a_merge_no_slot_holds_more_than_99()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 98), Stack(3, EmberrootSeed, 98));

        Assert.True(backpack.Move(2, 3));

        Assert.All(backpack.Slots, slot => Assert.InRange(slot.Quantity, 0, Backpack.MaxStack));
        Assert.Equal("3:Seed:Emberroot:99:-", Slot(backpack, 3));
        Assert.Equal("2:Seed:Emberroot:97:-", Slot(backpack, 2));
    }

    [Fact]
    public void BR_K2_A_move_works_across_rows_of_a_36_slot_backpack()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10));
        backpack.RaiseCapacity(36);

        Assert.True(backpack.Move(0, 35));
        Assert.True(backpack.Move(2, 20));
        Assert.True(backpack.Move(20, 35));

        Assert.Equal("35:Seed:Emberroot:10:-", Slot(backpack, 35));
        Assert.Equal("20:Tool:Hoe:1:Basic", Slot(backpack, 20));
        Assert.Equal("0:-:-:0:-", Slot(backpack, 0));
        Assert.Equal("2:-:-:0:-", Slot(backpack, 2));
    }

    [Fact]
    public void BR_K2_A_move_works_across_rows_of_a_24_slot_backpack()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.RaiseCapacity(24);

        Assert.True(backpack.Move(1, 18));

        Assert.Equal("18:Tool:WateringCan:1:Basic", Slot(backpack, 18));
        Assert.Equal("1:-:-:0:-", Slot(backpack, 1));
    }

    // ---- BR-K6: tools stay, once each ----

    [Fact]
    public void BR_K6_Tools_moved_around_are_still_each_held_once_with_their_tiers()
    {
        var backpack = TestGames.RestoredBackpack(Stack(2, EmberrootSeed, 10));
        backpack.SetTier(ToolId.Hoe, ToolTier.Groth);

        Assert.True(backpack.Move(0, 6));
        Assert.True(backpack.Move(1, 0));
        Assert.True(backpack.Move(2, 6));
        Assert.True(backpack.Move(6, 11));

        Assert.Single(backpack.Slots, slot => slot.Item == HoeItem);
        Assert.Single(backpack.Slots, slot => slot.Item == CanItem);
        Assert.Equal("2:Tool:Hoe:1:Groth", Slot(backpack, 2));
        Assert.Equal("0:Tool:WateringCan:1:Basic", Slot(backpack, 0));
        Assert.Equal("11:Seed:Emberroot:10:-", Slot(backpack, 11));
    }

    [Fact]
    public void BR_K6_A_tool_never_merges_and_its_slot_keeps_quantity_1()
    {
        var backpack = TestGames.ToolsOnly();

        Assert.True(backpack.Move(0, 1));
        Assert.True(backpack.Move(1, 0));

        Assert.All(
            backpack.Slots.Where(slot => slot.Item is { Kind: ItemKind.Tool }),
            slot => Assert.Equal(1, slot.Quantity));
        Assert.Equal("0:Tool:Hoe:1:Basic", Slot(backpack, 0));
        Assert.Equal("1:Tool:WateringCan:1:Basic", Slot(backpack, 1));
    }

    private static void AssertValidSlot(BackpackSlot slot, string context)
    {
        switch (slot.Item)
        {
            case null:
                Assert.True(slot.Quantity == 0 && slot.ToolTier is null, $"empty Slot {slot.Index} not clean {context}");
                break;
            case { Kind: ItemKind.Tool }:
                Assert.True(slot.Quantity == 1 && slot.ToolTier is not null, $"tool Slot {slot.Index} invalid {context}");
                break;
            default:
                Assert.True(
                    slot.Quantity is >= 1 and <= Backpack.MaxStack && slot.ToolTier is null,
                    $"Slot {slot.Index} holds {slot.Quantity} {context}");
                break;
        }
    }
}
