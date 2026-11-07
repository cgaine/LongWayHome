using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Players;

/// <summary>The slot Backpack (#128): BR-K1, BR-K2, BR-K3, BR-K6.</summary>
public class BackpackTests
{
    private static readonly ItemRef EmberrootSeed = ItemRef.Seed(CropTypeId.Emberroot);

    private static readonly ItemRef DusktuberSeed = ItemRef.Seed(CropTypeId.Dusktuber);

    private static readonly ItemRef EmberrootCrop = ItemRef.Crop(CropTypeId.Emberroot);

    private static readonly ItemRef DusktuberCrop = ItemRef.Crop(CropTypeId.Dusktuber);

    private const string Hoe = "0:Tool:Hoe:1:Basic";

    private const string Can = "1:Tool:WateringCan:1:Basic";

    private static string Empty(int index) => $"{index}:-:-:0:-";

    /// <summary>The non-empty Slots, described.</summary>
    private static string[] Filled(Backpack backpack) =>
        backpack.Slots.Where(slot => slot.Item is not null).Select(TestGames.DescribeSlot).ToArray();

    [Fact]
    public void BR_K1_A_new_backpack_has_12_empty_slots_indexed_0_to_11_on_hotbar_row_0()
    {
        var backpack = new Backpack();

        Assert.Equal(12, backpack.Capacity);
        Assert.Equal(Enumerable.Range(0, 12), backpack.Slots.Select(slot => slot.Index));
        Assert.All(backpack.Slots, slot =>
        {
            Assert.Null(slot.Item);
            Assert.Equal(0, slot.Quantity);
            Assert.Null(slot.ToolTier);
        });
        Assert.Equal(0, backpack.HotbarRow);
    }

    [Fact]
    public void BR_K1_Raising_the_capacity_to_24_appends_12_empty_slots_and_keeps_the_contents()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 10));
        var before = TestGames.Describe(backpack);

        backpack.RaiseCapacity(24);

        Assert.Equal(24, backpack.Capacity);
        Assert.Equal(Enumerable.Range(0, 24), backpack.Slots.Select(slot => slot.Index));
        Assert.Equal(before, string.Join(",", backpack.Slots.Take(12).Select(TestGames.DescribeSlot)));
        Assert.Equal(Enumerable.Range(12, 12).Select(Empty), backpack.Slots.Skip(12).Select(TestGames.DescribeSlot));
    }

    [Fact]
    public void BR_K1_Raising_the_capacity_from_24_to_36_appends_12_more_empty_slots()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.RaiseCapacity(24);

        backpack.RaiseCapacity(36);

        Assert.Equal(36, backpack.Capacity);
        Assert.Equal(Enumerable.Range(0, 36), backpack.Slots.Select(slot => slot.Index));
        Assert.Equal([Hoe, Can], Filled(backpack));
    }

    [Theory]
    [InlineData(12, 12)]
    [InlineData(12, 6)]
    [InlineData(12, 13)]
    [InlineData(12, 48)]
    [InlineData(24, 12)]
    [InlineData(24, 24)]
    [InlineData(36, 24)]
    public void BR_K1_Capacity_only_rises_to_24_or_36_and_never_goes_down(int from, int to)
    {
        var backpack = TestGames.ToolsOnly();
        if (from >= 24)
        {
            backpack.RaiseCapacity(24);
        }

        if (from == 36)
        {
            backpack.RaiseCapacity(36);
        }

        var before = TestGames.Describe(backpack);

        Assert.ThrowsAny<ArgumentException>(() => backpack.RaiseCapacity(to));
        Assert.Equal(from, backpack.Capacity);
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void AC2_Add_tops_up_an_existing_stack_below_99_before_using_a_free_slot()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 10));

        var left = backpack.Add(EmberrootSeed, 5);

        Assert.Equal(0, left);
        Assert.Equal([Hoe, Can, "2:Seed:Emberroot:15:-"], Filled(backpack));
    }

    [Fact]
    public void BR_K3_Add_tops_up_the_lowest_index_stack_first_when_several_are_below_99()
    {
        var backpack = new Backpack(
            12,
            0,
            [
                new BackpackSlot(0, ItemRef.Tool(ToolId.Hoe), 1, ToolTier.Basic),
                new BackpackSlot(1, ItemRef.Tool(ToolId.WateringCan), 1, ToolTier.Basic),
                new BackpackSlot(2, EmberrootSeed, 50, null),
                new BackpackSlot(3, DusktuberSeed, 5, null),
                new BackpackSlot(4, EmberrootSeed, 50, null),
                .. Enumerable.Range(5, 7).Select(index => new BackpackSlot(index, null, 0, null)),
            ]);

        var left = backpack.Add(EmberrootSeed, 60);

        Assert.Equal(0, left);
        Assert.Equal(
            [Hoe, Can, "2:Seed:Emberroot:99:-", "3:Seed:Dusktuber:5:-", "4:Seed:Emberroot:61:-"],
            Filled(backpack));
    }

    [Fact]
    public void BR_K3_A_new_item_goes_into_the_lowest_free_slot()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 10), (DusktuberSeed, 5), (DusktuberCrop, 2));
        backpack.Remove(EmberrootSeed, 10);

        var left = backpack.Add(EmberrootCrop, 1);

        Assert.Equal(0, left);
        Assert.Equal(
            [Hoe, Can, "2:Crop:Emberroot:1:-", "3:Seed:Dusktuber:5:-", "4:Crop:Dusktuber:2:-"],
            Filled(backpack));
    }

    [Fact]
    public void AC3_Add_fills_the_existing_stack_to_99_then_puts_the_rest_in_the_lowest_free_slot()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 90), (DusktuberCrop, 3));

        var left = backpack.Add(EmberrootSeed, 20);

        Assert.Equal(0, left);
        Assert.Equal(
            [Hoe, Can, "2:Seed:Emberroot:99:-", "3:Crop:Dusktuber:3:-", "4:Seed:Emberroot:11:-"],
            Filled(backpack));
    }

    [Fact]
    public void BR_K2_A_quantity_above_99_spreads_over_several_slots_of_at_most_99()
    {
        var backpack = TestGames.ToolsOnly();

        var left = backpack.Add(EmberrootSeed, 250);

        Assert.Equal(0, left);
        Assert.Equal(
            [Hoe, Can, "2:Seed:Emberroot:99:-", "3:Seed:Emberroot:99:-", "4:Seed:Emberroot:52:-"],
            Filled(backpack));
        Assert.Equal(250, backpack.Quantity(EmberrootSeed));
    }

    [Fact]
    public void BR_K2_A_tool_cannot_be_added_as_an_item()
    {
        var backpack = TestGames.ToolsOnly();
        var before = TestGames.Describe(backpack);

        var thrown = Record.Exception(() => backpack.Add(ItemRef.Tool(ToolId.Hoe), 1));

        Assert.True(
            thrown is ArgumentException or InvalidOperationException,
            $"Expected ArgumentException or InvalidOperationException, got {thrown?.GetType().Name ?? "nothing"}.");
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void AC4_Add_places_what_fits_and_returns_the_quantity_that_fits_nowhere()
    {
        var backpack = TestGames.ToolsOnly();

        var left = backpack.Add(EmberrootCrop, (10 * 99) + 5);

        Assert.Equal(5, left);
        Assert.Equal(990, backpack.Quantity(EmberrootCrop));
        Assert.All(backpack.Slots.Skip(2), slot => Assert.Equal("Crop:Emberroot:99", $"{slot.Item?.Kind}:{slot.Item?.CropTypeId}:{slot.Quantity}"));
    }

    [Fact]
    public void AC4_Add_returns_0_when_everything_fits()
    {
        var backpack = TestGames.StartingBackpack();

        Assert.Equal(0, backpack.Add(DusktuberCrop, 1));
    }

    [Theory]
    [InlineData("crop_emberroot", 10, true)]
    [InlineData("crop_emberroot", 11, false)]
    [InlineData("seed_dusktuber", 1, false)]
    [InlineData("crop_emberroot", 1, true)]
    public void AC4_CanFit_predicts_whether_Add_would_place_everything_without_changing_anything(
        string itemName, int quantity, bool expected)
    {
        var item = itemName == "crop_emberroot" ? EmberrootCrop : DusktuberSeed;
        var backpack = TestGames.BackpackWith((EmberrootCrop, (10 * 99) - 10));
        var before = TestGames.Describe(backpack);

        var fits = backpack.CanFit(item, quantity);

        Assert.Equal(expected, fits);
        Assert.Equal(before, TestGames.Describe(backpack));
        Assert.Equal(expected, backpack.Add(item, quantity) == 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Adding_0_or_a_negative_quantity_throws(int quantity)
    {
        var backpack = TestGames.StartingBackpack();

        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.Add(EmberrootSeed, quantity));
    }

    [Fact]
    public void AC5_AddTool_puts_the_tool_alone_in_the_lowest_free_slot_with_quantity_1_and_its_tier()
    {
        var backpack = new Backpack();

        var added = backpack.AddTool(ToolId.Hoe, ToolTier.Basic);
        var second = backpack.AddTool(ToolId.WateringCan, ToolTier.Rosk);

        Assert.True(added);
        Assert.True(second);
        Assert.Equal([Hoe, "1:Tool:WateringCan:1:Rosk"], Filled(backpack));
        Assert.Equal(ToolTier.Basic, backpack.TierOf(ToolId.Hoe));
        Assert.Equal(ToolTier.Rosk, backpack.TierOf(ToolId.WateringCan));
        Assert.Equal(1, backpack.Quantity(ItemRef.Tool(ToolId.Hoe)));
    }

    [Fact]
    public void AC5_AddTool_with_no_free_slot_returns_false_and_changes_nothing()
    {
        var backpack = new Backpack();
        backpack.Add(EmberrootCrop, 12 * 99);
        var before = TestGames.Describe(backpack);

        var added = backpack.AddTool(ToolId.Hoe, ToolTier.Basic);

        Assert.False(added);
        Assert.Equal(before, TestGames.Describe(backpack));
        Assert.Null(backpack.TierOf(ToolId.Hoe));
    }

    [Fact]
    public void AC6_Removing_a_tool_throws_and_the_tool_stays()
    {
        var backpack = TestGames.StartingBackpack();
        var before = TestGames.Describe(backpack);

        Assert.Throws<InvalidOperationException>(() => backpack.Remove(ItemRef.Tool(ToolId.WateringCan), 1));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void BR_K6_Removing_every_item_leaves_the_tools_in_their_slots()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 10), (EmberrootCrop, 150));

        Assert.True(backpack.Remove(EmberrootSeed, 10));
        Assert.True(backpack.Remove(EmberrootCrop, 150));

        Assert.Equal([Hoe, Can], Filled(backpack));
        Assert.Equal(1, backpack.Quantity(ItemRef.Tool(ToolId.Hoe)));
        Assert.Equal(1, backpack.Quantity(ItemRef.Tool(ToolId.WateringCan)));
    }

    [Fact]
    public void AC7_Removing_more_than_is_held_returns_false_and_changes_nothing()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 150));
        var before = TestGames.Describe(backpack);

        var removed = backpack.Remove(EmberrootSeed, 151);

        Assert.False(removed);
        Assert.Equal(before, TestGames.Describe(backpack));
        Assert.False(backpack.Remove(DusktuberSeed, 1));
    }

    [Fact]
    public void AC7_Remove_takes_from_the_highest_index_slots_first()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 150), (DusktuberSeed, 5));

        var removed = backpack.Remove(EmberrootSeed, 30);

        Assert.True(removed);
        Assert.Equal(
            [Hoe, Can, "2:Seed:Emberroot:99:-", "3:Seed:Emberroot:21:-", "4:Seed:Dusktuber:5:-"],
            Filled(backpack));
    }

    [Fact]
    public void AC7_Remove_empties_a_slot_that_reaches_0_and_carries_on_into_the_next()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 150));

        var removed = backpack.Remove(EmberrootSeed, 60);

        Assert.True(removed);
        Assert.Equal("2:Seed:Emberroot:90:-", TestGames.DescribeSlot(backpack.Slots[2]));
        Assert.Equal(Empty(3), TestGames.DescribeSlot(backpack.Slots[3]));
        Assert.Equal(90, backpack.Quantity(EmberrootSeed));
    }

    [Fact]
    public void Quantity_totals_an_item_across_slots_and_is_0_when_not_held()
    {
        var backpack = TestGames.BackpackWith((EmberrootSeed, 120), (DusktuberSeed, 5), (EmberrootCrop, 3));

        Assert.Equal(120, backpack.Quantity(EmberrootSeed));
        Assert.Equal(5, backpack.Quantity(DusktuberSeed));
        Assert.Equal(3, backpack.Quantity(EmberrootCrop));
        Assert.Equal(0, backpack.Quantity(DusktuberCrop));
    }

    [Fact]
    public void SetTier_raises_a_tools_tier_on_its_slot()
    {
        var backpack = TestGames.ToolsOnly();

        backpack.SetTier(ToolId.WateringCan, ToolTier.Rosk);

        Assert.Equal(ToolTier.Rosk, backpack.TierOf(ToolId.WateringCan));
        Assert.Equal([Hoe, "1:Tool:WateringCan:1:Rosk"], Filled(backpack));
    }

    [Fact]
    public void SetTier_to_a_lower_tier_throws_and_keeps_the_tier()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.SetTier(ToolId.WateringCan, ToolTier.Groth);

        var thrown = Record.Exception(() => backpack.SetTier(ToolId.WateringCan, ToolTier.Rosk));

        Assert.True(
            thrown is ArgumentException or InvalidOperationException,
            $"Expected ArgumentException or InvalidOperationException, got {thrown?.GetType().Name ?? "nothing"}.");
        Assert.Equal(ToolTier.Groth, backpack.TierOf(ToolId.WateringCan));
    }

    [Fact]
    public void TierOf_a_tool_not_held_is_null()
    {
        var backpack = new Backpack();

        Assert.Null(backpack.TierOf(ToolId.Hoe));
    }

    // ---- #129: the hotbar row and taking from one Slot ----

    private static Backpack Restored(params BackpackSlot[] slots) => TestGames.RestoredBackpack(slots);

    private static Backpack Distinct() => TestGames.DistinctBackpack();

    [Theory]
    [InlineData(12, new[] { 1, 0 })]
    [InlineData(24, new[] { 1, 2, 3, 0 })]
    [InlineData(36, new[] { 1, 2, 3, 4, 5, 0 })]
    public void BR_K5_Cycling_moves_to_the_next_row_and_wraps_after_capacity_over_6_rows(int capacity, int[] rows)
    {
        var backpack = TestGames.ToolsOnly();
        if (capacity > 12)
        {
            backpack.RaiseCapacity(24);
        }

        if (capacity > 24)
        {
            backpack.RaiseCapacity(36);
        }

        var seen = new List<int>();
        foreach (var _ in rows)
        {
            backpack.CycleHotbar();
            seen.Add(backpack.HotbarRow);
        }

        Assert.Equal(rows, seen);
    }

    [Fact]
    public void BR_K5_The_hotbar_slots_are_the_six_slots_of_the_current_row()
    {
        var backpack = Distinct();

        var row0 = backpack.HotbarSlots.Select(slot => slot.Index).ToList();
        backpack.CycleHotbar();
        var row1 = backpack.HotbarSlots.Select(slot => slot.Index).ToList();
        backpack.CycleHotbar();
        var again = backpack.HotbarSlots.Select(slot => slot.Index).ToList();

        Assert.Equal(Enumerable.Range(0, 6), row0);
        Assert.Equal(Enumerable.Range(6, 6), row1);
        Assert.Equal(Enumerable.Range(0, 6), again);
    }

    [Fact]
    public void BR_K5_The_hotbar_slots_carry_their_contents()
    {
        var backpack = Distinct();
        backpack.CycleHotbar();

        Assert.Equal(
            backpack.Slots.Skip(6).Take(6).Select(TestGames.DescribeSlot),
            backpack.HotbarSlots.Select(TestGames.DescribeSlot));
    }

    [Fact]
    public void BR_K5_With_24_slots_the_third_row_is_slots_12_to_17()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.RaiseCapacity(24);

        backpack.CycleHotbar();
        backpack.CycleHotbar();

        Assert.Equal(2, backpack.HotbarRow);
        Assert.Equal(Enumerable.Range(12, 6), backpack.HotbarSlots.Select(slot => slot.Index));
    }

    [Fact]
    public void BR_K5_Cycling_changes_no_slot()
    {
        var backpack = Distinct();
        var before = TestGames.Describe(backpack);

        backpack.CycleHotbar();

        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void BR_K6_RemoveFrom_a_tool_slot_returns_false_and_the_tool_stays()
    {
        var backpack = TestGames.ToolsOnly();
        var before = TestGames.Describe(backpack);

        Assert.False(backpack.RemoveFrom(0, 1));
        Assert.False(backpack.RemoveFrom(1, 1));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void RemoveFrom_takes_only_from_the_given_slot()
    {
        var backpack = Restored(new BackpackSlot(2, EmberrootSeed, 5, null), new BackpackSlot(3, EmberrootSeed, 5, null));

        Assert.True(backpack.RemoveFrom(2, 1));

        Assert.Equal(4, backpack.Slots[2].Quantity);
        Assert.Equal(5, backpack.Slots[3].Quantity);
        Assert.Equal(EmberrootSeed, backpack.Slots[2].Item);
    }

    [Fact]
    public void RemoveFrom_with_fewer_in_the_slot_returns_false_and_changes_nothing()
    {
        var backpack = Restored(new BackpackSlot(2, EmberrootSeed, 2, null), new BackpackSlot(3, EmberrootSeed, 5, null));
        var before = TestGames.Describe(backpack);

        Assert.False(backpack.RemoveFrom(2, 3));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void RemoveFrom_an_empty_slot_returns_false_and_changes_nothing()
    {
        var backpack = TestGames.ToolsOnly();
        var before = TestGames.Describe(backpack);

        Assert.False(backpack.RemoveFrom(5, 1));
        Assert.Equal(before, TestGames.Describe(backpack));
    }

    [Fact]
    public void RemoveFrom_empties_a_slot_that_reaches_0()
    {
        var backpack = Restored(new BackpackSlot(4, DusktuberCrop, 3, null));

        Assert.True(backpack.RemoveFrom(4, 3));

        Assert.Equal(Empty(4), TestGames.DescribeSlot(backpack.Slots[4]));
    }
}
