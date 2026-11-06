using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Players;

public class InventoryStackTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void BR_P7_inventory_stack_with_quantity_below_1_throws(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryStack(ItemKind.Seed, CropTypeId.Emberroot, quantity));
    }

    [Fact]
    public void BR_P7_inventory_stack_of_quantity_1_is_created()
    {
        var stack = new InventoryStack(ItemKind.Crop, CropTypeId.Sunbell, 1);

        Assert.Equal(ItemKind.Crop, stack.Kind);
        Assert.Equal(CropTypeId.Sunbell, stack.CropTypeId);
        Assert.Equal(1, stack.Quantity);
    }
}
