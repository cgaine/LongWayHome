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
    public void Row_of_a_seed_listing_with_no_crop_type_throws()
    {
        var listing = new ShopListing("seed_nothing", ShopItemKind.Seed, null, new Gold(10));

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

    [Fact]
    public void AC2_no_seeds_text_is_no_seeds_this_season()
    {
        Assert.Equal("No seeds this season", ShopText.NoSeeds);
    }
}
