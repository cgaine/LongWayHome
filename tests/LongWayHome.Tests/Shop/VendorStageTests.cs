using LongWayHome.Application.Games;
using LongWayHome.Domain.Buildings;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// #163 AC8: <see cref="ShopService.VendorStage"/> reads the vendor Building's stage, so a listing's
/// <c>requiresStage</c> is checked against the Building (BR-L5).
/// </summary>
public class VendorStageTests
{
    private static readonly Application.Data.GameData WithStage2Listing = TestGames.Shipped with
    {
        ShopListings =
        [
            .. TestGames.Shipped.ShopListings,
            new ShopListing("store_stage_2", ShopItemKind.Seed, CropTypeId.Emberroot, new Gold(15), BuildingId.GeneralStore, 2, null, false),
        ],
    };

    private static List<Building> Buildings(int store, int carpenter = 0, int mirasHouse = 0, int blacksmith = 0) =>
    [
        new(BuildingId.GeneralStore, store),
        new(BuildingId.Carpenter, carpenter),
        new(BuildingId.MirasHouse, mirasHouse),
        new(BuildingId.Blacksmith, blacksmith),
    ];

    [Theory]
    [InlineData(BuildingId.GeneralStore, 3)]
    [InlineData(BuildingId.Carpenter, 1)]
    [InlineData(BuildingId.MirasHouse, 1)]
    [InlineData(BuildingId.Blacksmith, 2)]
    public void AC8_VendorStage_returns_the_buildings_stage(BuildingId vendor, int stage)
    {
        var game = BuildingGames.WithBuildings(Buildings(store: 3, carpenter: 1, mirasHouse: 1, blacksmith: 2));

        Assert.Equal(stage, ShopService.VendorStage(game, vendor));
    }

    [Fact]
    public void BR_L5_A_store_at_stage_2_offers_a_listing_that_requires_stage_2()
    {
        var game = BuildingGames.WithBuildings(Buildings(store: 2), gold: 1000);

        Assert.Contains("store_stage_2", ShopService.Offers(game, WithStage2Listing).Select(listing => listing.Id));
    }

    [Fact]
    public void BR_L5_A_store_at_stage_1_does_not_offer_a_listing_that_requires_stage_2()
    {
        var game = BuildingGames.WithBuildings(Buildings(store: 1), gold: 1000);
        Assert.Equal(1, game.BuildingStage(BuildingId.GeneralStore));

        Assert.DoesNotContain("store_stage_2", ShopService.Offers(game, WithStage2Listing).Select(listing => listing.Id));
    }

    [Fact]
    public void A_new_games_shop_offers_are_unchanged_at_stage_1()
    {
        var session = TestGames.NewSession();
        Assert.Equal(1, session.Game.BuildingStage(BuildingId.GeneralStore));

        var offers = session.ShopOffers().Select(listing => listing.Id).ToList();
        Assert.Equal(
            ["seed_emberroot", "seed_dusktuber", "tool_axe", "tool_pickaxe"],
            offers.Where(id => id.StartsWith("seed_", StringComparison.Ordinal) || id.StartsWith("tool_", StringComparison.Ordinal)));
    }
}
