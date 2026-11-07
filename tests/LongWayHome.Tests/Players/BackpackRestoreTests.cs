using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Players;

/// <summary>
/// The Backpack's restore constructor (#128), which the save loader relies on to refuse a broken Backpack (BR-K1,
/// BR-K2, BR-K6), plus the smaller operations the AC tests don't reach.
/// </summary>
public class BackpackRestoreTests
{
    private static BackpackSlot Hoe(int index = 0) => new(index, ItemRef.Tool(ToolId.Hoe), 1, ToolTier.Basic);

    private static BackpackSlot Can(int index = 1) => new(index, ItemRef.Tool(ToolId.WateringCan), 1, ToolTier.Basic);

    private static BackpackSlot Empty(int index) => new(index, null, 0, null);

    /// <summary>The hoe, the can and empty Slots up to the capacity, with any Slot replaced by the overrides.</summary>
    private static List<BackpackSlot> Slots(int capacity, params BackpackSlot[] overrides)
    {
        var slots = new List<BackpackSlot> { Hoe(), Can() };
        slots.AddRange(Enumerable.Range(2, capacity - 2).Select(Empty));
        foreach (var slot in overrides)
        {
            slots[slot.Index] = slot;
        }

        return slots;
    }

    [Theory]
    [InlineData(12, 0)]
    [InlineData(12, 1)]
    [InlineData(24, 3)]
    [InlineData(36, 5)]
    public void BR_K1_A_restored_backpack_keeps_its_capacity_hotbar_row_and_slots(int capacity, int hotbarRow)
    {
        var slots = Slots(capacity, new BackpackSlot(2, ItemRef.Seed(CropTypeId.Emberroot), 99, null));

        var backpack = new Backpack(capacity, hotbarRow, slots);

        Assert.Equal(capacity, backpack.Capacity);
        Assert.Equal(hotbarRow, backpack.HotbarRow);
        Assert.Equal(slots, backpack.Slots);
    }

    [Fact]
    public void A_restored_backpack_orders_its_slots_by_index_whatever_order_they_come_in()
    {
        var slots = Slots(12);
        slots.Reverse();

        var backpack = new Backpack(12, 0, slots);

        Assert.Equal(Enumerable.Range(0, 12), backpack.Slots.Select(slot => slot.Index));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(13)]
    [InlineData(48)]
    public void BR_K1_A_capacity_other_than_12_24_or_36_is_refused(int capacity) =>
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(capacity, 0, Slots(Math.Max(capacity, 2))));

    [Theory]
    [InlineData(12, -1)]
    [InlineData(12, 2)]
    [InlineData(24, 4)]
    [InlineData(36, 6)]
    public void A_hotbar_row_outside_0_to_capacity_over_6_minus_1_is_refused(int capacity, int hotbarRow) =>
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(capacity, hotbarRow, Slots(capacity)));

    [Fact]
    public void BR_K1_A_slot_index_outside_the_capacity_is_refused()
    {
        var slots = Slots(12);
        slots[11] = Empty(12);

        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, slots));
    }

    [Fact]
    public void BR_K1_A_repeated_slot_index_is_refused()
    {
        var slots = Slots(12);
        slots[11] = Empty(10);

        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, slots));
    }

    [Fact]
    public void BR_K1_Fewer_slots_than_the_capacity_is_refused() =>
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, Slots(12).Take(11)));

    [Fact]
    public void Null_slots_or_a_null_slot_are_refused()
    {
        var slots = Slots(12);
        slots[5] = null!;

        Assert.Throws<ArgumentNullException>(() => new Backpack(12, 0, null!));
        Assert.Throws<ArgumentNullException>(() => new Backpack(12, 0, slots));
    }

    public static TheoryData<string, BackpackSlot> BrokenSlots => new()
    {
        { "empty with a quantity", new BackpackSlot(5, null, 1, null) },
        { "empty with a tier", new BackpackSlot(5, null, 0, ToolTier.Basic) },
        { "item with 0", new BackpackSlot(5, ItemRef.Seed(CropTypeId.Emberroot), 0, null) },
        { "item with 100", new BackpackSlot(5, ItemRef.Crop(CropTypeId.Emberroot), 100, null) },
        { "item with a tier", new BackpackSlot(5, ItemRef.Seed(CropTypeId.Emberroot), 3, ToolTier.Rosk) },
        { "tool with 2", new BackpackSlot(0, ItemRef.Tool(ToolId.Hoe), 2, ToolTier.Basic) },
        { "tool with no tier", new BackpackSlot(0, ItemRef.Tool(ToolId.Hoe), 1, null) },
        { "seed with a tool id", new BackpackSlot(5, new ItemRef(ItemKind.Seed, null, ToolId.Hoe), 1, null) },
        { "tool with a crop id", new BackpackSlot(5, new ItemRef(ItemKind.Tool, CropTypeId.Emberroot, null), 1, ToolTier.Basic) },
        { "item with both ids", new BackpackSlot(5, new ItemRef(ItemKind.Crop, CropTypeId.Emberroot, ToolId.Hoe), 1, null) },
        { "undefined kind", new BackpackSlot(5, new ItemRef((ItemKind)99, CropTypeId.Emberroot, null), 1, null) },
    };

    [Theory]
    [MemberData(nameof(BrokenSlots))]
    public void BR_K2_A_slot_that_breaks_the_slot_rule_is_refused(string because, BackpackSlot slot)
    {
        Assert.NotNull(because);

        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, Slots(12, slot)));
    }

    [Fact]
    public void BR_K6_A_backpack_without_the_hoe_or_the_can_is_refused()
    {
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, Slots(12, Empty(0))));
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, Slots(12, Empty(1))));
    }

    [Fact]
    public void BR_K6_A_backpack_holding_a_tool_twice_is_refused()
    {
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, Slots(12, Hoe(4))));
        Assert.ThrowsAny<ArgumentException>(() => new Backpack(12, 0, Slots(12, Can(4))));
    }

    [Fact]
    public void SetTier_on_a_tool_not_held_throws()
    {
        var backpack = new Backpack();

        Assert.Throws<InvalidOperationException>(() => backpack.SetTier(ToolId.Hoe, ToolTier.Rosk));
    }

    [Fact]
    public void SetTier_to_the_same_tier_keeps_it()
    {
        var backpack = TestGames.ToolsOnly();

        backpack.SetTier(ToolId.Hoe, ToolTier.Basic);

        Assert.Equal(ToolTier.Basic, backpack.TierOf(ToolId.Hoe));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Removing_or_fitting_0_or_a_negative_quantity_throws(int quantity)
    {
        var backpack = TestGames.StartingBackpack();

        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.Remove(ItemRef.Seed(CropTypeId.Emberroot), quantity));
        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.CanFit(ItemRef.Seed(CropTypeId.Emberroot), quantity));
    }

    [Fact]
    public void BR_K2_CanFit_a_tool_throws()
    {
        var backpack = TestGames.ToolsOnly();

        Assert.Throws<ArgumentException>(() => backpack.CanFit(ItemRef.Tool(ToolId.Hoe), 1));
    }

    [Fact]
    public void Totals_are_per_crop_type_across_slots_in_crop_type_order_for_one_kind()
    {
        var backpack = TestGames.BackpackWith(
            (ItemRef.Crop(CropTypeId.Moonmelon), 1),
            (ItemRef.Seed(CropTypeId.Emberroot), 3),
            (ItemRef.Crop(CropTypeId.Emberroot), 150));

        Assert.Equal([(CropTypeId.Emberroot, 150), (CropTypeId.Moonmelon, 1)], backpack.Totals(ItemKind.Crop));
        Assert.Equal([(CropTypeId.Emberroot, 3)], backpack.Totals(ItemKind.Seed));
        Assert.Empty(backpack.Totals(ItemKind.Tool));
    }

    [Theory]
    [InlineData(ItemKind.Seed, true, false, true)]
    [InlineData(ItemKind.Crop, true, false, true)]
    [InlineData(ItemKind.Tool, false, true, true)]
    [InlineData(ItemKind.Seed, false, true, false)]
    [InlineData(ItemKind.Tool, true, false, false)]
    [InlineData(ItemKind.Seed, false, false, false)]
    [InlineData(ItemKind.Crop, true, true, false)]
    public void An_item_ref_is_valid_only_with_exactly_the_id_its_kind_needs(
        ItemKind kind, bool hasCrop, bool hasTool, bool expected)
    {
        var item = new ItemRef(kind, hasCrop ? CropTypeId.Emberroot : null, hasTool ? ToolId.Hoe : null);

        Assert.Equal(expected, item.IsValid);
    }

    [Fact]
    public void The_item_ref_factories_are_valid()
    {
        Assert.True(ItemRef.Seed(CropTypeId.Dusktuber).IsValid);
        Assert.True(ItemRef.Crop(CropTypeId.Dusktuber).IsValid);
        Assert.True(ItemRef.Tool(ToolId.WateringCan).IsValid);
    }
}
