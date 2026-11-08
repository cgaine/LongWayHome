using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// The paths of #146's Game sell methods the acceptance tests don't drive: a missing Crop data table throws before
/// anything changes, as <c>ShippingPayout</c> does.
/// </summary>
public class SellToMaggieGuardTests
{
    [Fact]
    public void Sell_methods_reject_a_null_crop_table_and_change_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((ItemRef.Crop(CropTypeId.Emberroot), 3)), gold: 100);

        Assert.Throws<ArgumentNullException>(() => game.SellCrop(CropTypeId.Emberroot, null!));
        Assert.Throws<ArgumentNullException>(() => game.SellAllCrops(null!));
        Assert.Throws<ArgumentNullException>(() => game.SellPayout(CropTypeId.Emberroot, null!));
        Assert.Throws<ArgumentNullException>(() => game.SellAllPayout(null!));

        Assert.Equal(100, game.Player.Gold.Value);
        Assert.Equal(3, game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }
}
