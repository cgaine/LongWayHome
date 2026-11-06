using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Players;

public class InventoryTests
{
    [Fact]
    public void AC5_BR_P7_add_of_held_kind_and_crop_merges_into_the_existing_stack()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 10);

        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 3);

        Assert.Equal([(ItemKind.Seed, CropTypeId.Emberroot, 13)], Contents(inventory));
        Assert.Equal(13, inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
    }

    [Fact]
    public void AC5_BR_P7_remove_to_zero_removes_the_stack()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 5);

        var removed = inventory.Remove(ItemKind.Seed, CropTypeId.Dusktuber, 5);

        Assert.True(removed);
        Assert.Empty(inventory.Stacks);
        Assert.Equal(0, inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
    }

    [Fact]
    public void AC5_BR_P7_remove_of_part_of_a_stack_reduces_it()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 5);

        var removed = inventory.Remove(ItemKind.Seed, CropTypeId.Dusktuber, 2);

        Assert.True(removed);
        Assert.Equal([(ItemKind.Seed, CropTypeId.Dusktuber, 3)], Contents(inventory));
    }

    [Fact]
    public void AC6_BR_P7_remove_more_than_held_returns_false_and_changes_nothing()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 4);

        var removed = inventory.Remove(ItemKind.Seed, CropTypeId.Emberroot, 5);

        Assert.False(removed);
        Assert.Equal([(ItemKind.Seed, CropTypeId.Emberroot, 4)], Contents(inventory));
    }

    [Fact]
    public void AC6_BR_P7_remove_of_absent_stack_returns_false()
    {
        var inventory = new Inventory();

        var removed = inventory.Remove(ItemKind.Crop, CropTypeId.Moonmelon, 1);

        Assert.False(removed);
        Assert.Empty(inventory.Stacks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AC6_BR_P7_add_with_quantity_zero_or_less_throws(int quantity)
    {
        var inventory = new Inventory();

        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, quantity));
        Assert.Empty(inventory.Stacks);
    }

    [Fact]
    public void BR_P7_quantity_of_absent_stack_is_zero()
    {
        var inventory = new Inventory();

        Assert.Equal(0, inventory.Quantity(ItemKind.Crop, CropTypeId.Emberroot));
    }

    [Fact]
    public void BR_P7_stacks_are_ordered_by_kind_then_crop_type()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Crop, CropTypeId.Dusktuber, 1);
        inventory.Add(ItemKind.Seed, CropTypeId.Moonmelon, 2);
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 3);
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 4);

        Assert.Equal(
            [
                (ItemKind.Seed, CropTypeId.Emberroot, 4),
                (ItemKind.Seed, CropTypeId.Moonmelon, 2),
                (ItemKind.Crop, CropTypeId.Emberroot, 3),
                (ItemKind.Crop, CropTypeId.Dusktuber, 1),
            ],
            Contents(inventory));
    }

    [Fact]
    public void BR_P7_same_crop_as_seed_and_as_crop_are_separate_stacks()
    {
        var inventory = new Inventory();

        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 2);
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 5);

        Assert.Equal(
            [(ItemKind.Seed, CropTypeId.Emberroot, 2), (ItemKind.Crop, CropTypeId.Emberroot, 5)],
            Contents(inventory));
    }

    private static IEnumerable<(ItemKind, CropTypeId, int)> Contents(Inventory inventory) =>
        inventory.Stacks.Select(stack => (stack.Kind, stack.CropTypeId, stack.Quantity));
}
