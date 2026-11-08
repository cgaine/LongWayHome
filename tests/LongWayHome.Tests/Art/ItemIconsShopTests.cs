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
        var listing = new ShopListing($"seed_{crop}", ShopItemKind.Seed, crop, new Gold(10), BuildingId.GeneralStore, 1, null, false);

        Assert.Equal(ItemIcons.SeedIcon(crop), ItemIcons.For(listing));
    }

    [Fact]
    public void AC6_can_upgrade_listing_has_no_icon()
    {
        var listing = new ShopListing("can_upgrade", ShopItemKind.CanUpgrade, null, new Gold(200), BuildingId.GeneralStore, 1, null, true);

        Assert.Null(ItemIcons.For(listing));
    }

    [Fact]
    public void AC6_seed_listing_without_a_crop_type_has_no_icon()
    {
        var listing = new ShopListing("seed_nothing", ShopItemKind.Seed, null, new Gold(10), BuildingId.GeneralStore, 1, null, false);

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
            // #145: the axe and pickaxe show their tool icons; the Can upgrade has none.
            [
                ItemIcons.SeedIcon(CropTypeId.Emberroot),
                ItemIcons.SeedIcon(CropTypeId.Dusktuber),
                "res://art/icons/tool_axe.svg",
                "res://art/icons/tool_pickaxe.svg",
                null,
            ],
            icons);
    }

    // ---- #145: the axe and the pickaxe ----

    [Theory]
    [InlineData(ToolId.Axe, "tool_axe", "res://art/icons/tool_axe.svg")]
    [InlineData(ToolId.Pickaxe, "tool_pickaxe", "res://art/icons/tool_pickaxe.svg")]
    public void AC9_tool_listing_icon_is_its_tool_icon(ToolId tool, string id, string icon)
    {
        var listing = new ShopListing(id, ShopItemKind.Tool, null, new Gold(50), BuildingId.GeneralStore, 1, tool, true);

        Assert.Equal(icon, ItemIcons.For(listing));
    }

    [Theory]
    [InlineData(ToolId.Axe, "res://art/icons/tool_axe.svg")]
    [InlineData(ToolId.Pickaxe, "res://art/icons/tool_pickaxe.svg")]
    public void AC9_an_axe_or_pickaxe_item_has_its_tool_icon(ToolId tool, string icon)
    {
        Assert.Equal(icon, ItemIcons.For(ItemRef.Tool(tool)));
    }

    [Theory]
    [InlineData(ToolId.Hoe)]
    [InlineData(ToolId.WateringCan)]
    public void AC9_the_hoe_and_can_items_have_no_icon(ToolId tool)
    {
        Assert.Null(ItemIcons.For(ItemRef.Tool(tool)));
    }
}
