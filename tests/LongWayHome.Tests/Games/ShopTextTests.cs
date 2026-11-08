using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The shop menu's text (#45, AC9).</summary>
public class ShopTextTests
{
    private static ShopListing Listing(string id) => TestGames.Shipped.ShopListings.Single(listing => listing.Id == id);

    [Fact]
    public void AC9_row_shows_the_crop_name_and_price()
    {
        Assert.Equal("Emberroot seeds — 15g", ShopText.Row(Listing("seed_emberroot"), TestGames.Shipped));
    }

    [Fact]
    public void AC9_row_uses_the_listing_price()
    {
        Assert.Equal("Starcorn seeds — 45g", ShopText.Row(Listing("seed_starcorn"), TestGames.Shipped));
    }

    [Fact]
    public void AC9_row_falls_back_to_the_canonical_label_when_the_crop_type_has_no_data_row()
    {
        var data = TestGames.ShippedWithout(CropTypeId.Emberroot);

        Assert.Equal(
            $"{CropLabels.For(CropTypeId.Emberroot)} seeds — 15g",
            ShopText.Row(Listing("seed_emberroot"), data));
    }

    [Theory]
    [InlineData(85, "Gold 85")]
    [InlineData(100, "Gold 100")]
    [InlineData(0, "Gold 0")]
    public void AC9_gold_line_shows_the_amount(int gold, string expected)
    {
        Assert.Equal(expected, ShopText.Gold(gold));
    }

    [Theory]
    [InlineData(Rejection.NotEnoughGold, "Not enough gold")]
    [InlineData(Rejection.NotOffered, "Not sold this season")]
    public void AC9_rejection_text(Rejection rejection, string expected)
    {
        Assert.Equal(expected, ShopText.Rejection(rejection));
    }

    [Fact]
    public void AC9_A_purchase_refused_for_room_reads_Backpack_full()
    {
        Assert.Equal("Backpack full", ShopText.Rejection(Rejection.BackpackFull));
    }

    [Fact]
    public void AC4_after_buy_message_when_seeds_dropped()
    {
        Assert.Equal("Some seeds dropped at your feet", ShopText.SeedsDropped);
        Assert.Equal("Some seeds dropped at your feet", ShopText.AfterBuy(dropped: true));
    }

    [Fact]
    public void AC4_after_buy_message_is_empty_when_nothing_dropped()
    {
        Assert.Equal("", ShopText.AfterBuy(dropped: false));
    }

    [Fact]
    public void Row_of_a_seed_listing_with_no_crop_type_throws()
    {
        var listing = new ShopListing("seed_nothing", ShopItemKind.Seed, null, new Gold(10), BuildingId.GeneralStore, 1, null, false);

        Assert.Throws<ArgumentException>(() => ShopText.Row(listing, TestGames.Shipped));
    }

    // ---- #46: the Watering can upgrade ----

    [Fact]
    public void AC8_row_of_the_can_upgrade_is_watering_can_upgrade_200g()
    {
        Assert.Equal("Watering can upgrade — 200g", ShopText.Row(Listing("can_upgrade"), TestGames.Shipped));
    }

    [Fact]
    public void AC8_row_of_the_can_upgrade_uses_the_listing_price()
    {
        var listing = Listing("can_upgrade") with { Price = new Gold(350) };

        Assert.Equal("Watering can upgrade — 350g", ShopText.Row(listing, TestGames.Shipped));
    }

    [Fact]
    public void Rejection_text_of_a_non_shop_rejection_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShopText.Rejection(Rejection.TooTired));
    }

    // ---- #145: the axe and the pickaxe ----

    [Fact]
    public void AC9_row_of_the_axe_is_axe_50g()
    {
        Assert.Equal("Axe — 50g", ShopText.Row(Listing("tool_axe"), TestGames.Shipped));
    }

    [Fact]
    public void AC9_row_of_the_pickaxe_is_pickaxe_75g()
    {
        Assert.Equal("Pickaxe — 75g", ShopText.Row(Listing("tool_pickaxe"), TestGames.Shipped));
    }

    [Fact]
    public void AC9_row_of_a_tool_listing_uses_the_listing_price()
    {
        var listing = new ShopListing("tool_axe", ShopItemKind.Tool, null, new Gold(65), BuildingId.GeneralStore, 1, ToolId.Axe, true);

        Assert.Equal("Axe — 65g", ShopText.Row(listing, TestGames.Shipped));
    }

    [Fact]
    public void Row_of_a_tool_listing_with_no_tool_throws()
    {
        var listing = new ShopListing("tool_nothing", ShopItemKind.Tool, null, new Gold(50), BuildingId.GeneralStore, 1, null, true);

        Assert.Throws<ArgumentException>(() => ShopText.Row(listing, TestGames.Shipped));
    }

    [Fact]
    public void AC2_no_seeds_text_is_no_seeds_this_season()
    {
        Assert.Equal("No seeds this season", ShopText.NoSeeds);
    }

    // ---- #146: the Sell section ----

    [Theory]
    [InlineData(495, "Sell all crops — 495g")]
    [InlineData(0, "Sell all crops — 0g")]
    public void AC9_sell_all_row_shows_the_total(int total, string expected)
    {
        Assert.Equal(expected, ShopText.SellAllRow(total));
    }

    [Fact]
    public void AC9_sell_row_shows_name_quantity_and_total()
    {
        Assert.Equal("Emberroot × 12 — 420g", ShopText.SellRow("Emberroot", 12, 420));
    }

    [Fact]
    public void AC9_after_sell_shows_what_the_sale_paid()
    {
        Assert.Equal("Sold for 420g", ShopText.AfterSell(420));
    }

    [Fact]
    public void AC9_nothing_to_sell_text_is_no_crops_to_sell()
    {
        Assert.Equal("No crops to sell", ShopText.NothingToSell);
    }

    [Fact]
    public void AC9_sell_header_is_sell()
    {
        Assert.Equal("Sell", ShopText.SellHeader);
    }

    [Theory]
    [InlineData(Rejection.NothingToSell, "No crops to sell")]
    [InlineData(Rejection.NotSellable, "Maggie can't buy that")]
    public void AC9_sell_rejection_text(Rejection rejection, string expected)
    {
        Assert.Equal(expected, ShopText.Rejection(rejection));
    }
}
