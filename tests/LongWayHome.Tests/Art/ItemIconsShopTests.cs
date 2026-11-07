using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The icon lookups the shop and the crops row use beyond <see cref="ItemIconsTests"/> (#95): a Shop seed listing
/// shows its seed icon and the Watering can upgrade shows none (AC6), and the shop's offers follow the Season (BR-L1).
/// </summary>
public class ItemIconsShopTests
{
    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Moonmelon)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    public void AC6_seed_listing_icon_is_its_seed_icon(CropTypeId crop)
    {
        var listing = new ShopListing($"seed_{crop}", ShopItemKind.Seed, crop, new Gold(10));

        Assert.Equal(ItemIcons.SeedIcon(crop), ItemIcons.For(listing));
    }

    [Fact]
    public void AC6_can_upgrade_listing_has_no_icon()
    {
        var listing = new ShopListing("can_upgrade", ShopItemKind.CanUpgrade, null, new Gold(200));

        Assert.Null(ItemIcons.For(listing));
    }

    [Fact]
    public void AC6_seed_listing_without_a_crop_type_has_no_icon()
    {
        var listing = new ShopListing("seed_nothing", ShopItemKind.Seed, null, new Gold(10));

        Assert.Null(ItemIcons.For(listing));
    }

    [Fact]
    public void AC6_null_listing_throws() =>
        Assert.Throws<ArgumentNullException>(() => ItemIcons.For((ShopListing)null!));

    [Fact]
    public void AC1_null_held_item_throws() =>
        Assert.Throws<ArgumentNullException>(() => ItemIcons.For((HeldItem)null!));

    [Fact]
    public void BR_L1_spring_shop_icons_are_the_spring_seed_icons()
    {
        var session = TestGames.NewSession();

        var icons = session.ShopOffers().Select(ItemIcons.For).ToList();

        Assert.Equal(
            [ItemIcons.SeedIcon(CropTypeId.Emberroot), ItemIcons.SeedIcon(CropTypeId.Dusktuber), null],
            icons);
    }
}
