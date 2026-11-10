using System.Text.Json.Nodes;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Data;

/// <summary>
/// #166 AC5: <c>shop.json</c>'s <c>backpack_upgrade</c> and <c>placeable</c> listings, with their
/// <c>backpackCapacity</c> and <c>placeableId</c>. Each test starts from a small valid shop and breaks one thing.
/// </summary>
public class ShopDataTests
{
    private const string Crops = """
        {
          "cropTypes": [
            { "id": "emberroot", "name": "Emberroot", "growDays": 3, "seedCost": 15, "sellPrice": 35, "season": "spring", "regrows": false }
          ]
        }
        """;

    private const string Shop = """
        {
          "listings": [
            { "id": "seed_emberroot", "kind": "seed", "cropTypeId": "emberroot", "toolId": null, "placeableId": null, "backpackCapacity": null, "price": 15, "vendor": "general_store", "requiresStage": 1, "oncePerGame": false },
            { "id": "tool_axe", "kind": "tool", "cropTypeId": null, "toolId": "axe", "placeableId": null, "backpackCapacity": null, "price": 50, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true },
            { "id": "can_upgrade", "kind": "can_upgrade", "cropTypeId": null, "toolId": null, "placeableId": null, "backpackCapacity": null, "price": 200, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true },
            { "id": "backpack_24", "kind": "backpack_upgrade", "cropTypeId": null, "toolId": null, "placeableId": null, "backpackCapacity": 24, "price": 400, "vendor": "general_store", "requiresStage": 2, "oncePerGame": true },
            { "id": "backpack_36", "kind": "backpack_upgrade", "cropTypeId": null, "toolId": null, "placeableId": null, "backpackCapacity": 36, "price": 1000, "vendor": "general_store", "requiresStage": 3, "oncePerGame": true },
            { "id": "chest_store", "kind": "placeable", "cropTypeId": null, "toolId": null, "placeableId": "chest", "backpackCapacity": null, "price": 100, "vendor": "general_store", "requiresStage": 3, "oncePerGame": false }
          ]
        }
        """;

    private const string Tuning = """
        {
          "startingGold": 100,
          "startingSeeds": [ { "cropTypeId": "emberroot", "quantity": 10 } ],
          "maxEnergy": 150,
          "passOutEnergy": 75,
          "toolEnergyCost": 2,
          "canCapacity": 20,
          "canUpgradedCapacity": 40,
          "dayStartMinutes": 360,
          "dayEndMinutes": 1560,
          "dayLengthRealSeconds": 720,
          "rainChancePercent": 22,
          "seasonLengthDays": 28,
          "walkSpeedTilesPerSecond": 3.6,
          "runSpeedTilesPerSecond": 5.2
        }
        """;

    private const int Seed = 0;

    private const int Tool = 1;

    private const int CanUpgrade = 2;

    private const int Backpack24 = 3;

    private const int Backpack36 = 4;

    private const int Chest = 5;

    private static Application.Data.GameData Load(string shop) =>
        new JsonGameDataSource(Crops, shop, Tuning, """{ "spawns": [] }""").Load();

    private static IReadOnlyList<string> LoadErrors(string shop) =>
        Assert.Throws<GameDataException>(() => Load(shop)).Errors;

    private static string Edit(Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(Shop)!;
        edit(root);
        return root.ToJsonString();
    }

    private static string EditListing(int index, string field, JsonNode? value) =>
        Edit(root => root["listings"]![index]![field] = value);

    /// <summary>The single load error, checked to name the listing and the offending field.</summary>
    private static void AssertSingleErrorNaming(IReadOnlyList<string> errors, string listingId, string field)
    {
        var error = Assert.Single(errors);
        Assert.StartsWith($"shop.json: {listingId}.", error);
        Assert.Contains(field, error);
    }

    // ---- What loads ----

    [Fact]
    public void AC5_Backpack_upgrade_and_placeable_listings_load_with_their_capacity_and_placeable()
    {
        var data = Load(Shop);

        Assert.Equal(
            [
                new ShopListing("backpack_24", ShopItemKind.BackpackUpgrade, null, new Gold(400), BuildingId.GeneralStore, 2, null, true, null, 24),
                new ShopListing("backpack_36", ShopItemKind.BackpackUpgrade, null, new Gold(1000), BuildingId.GeneralStore, 3, null, true, null, 36),
                new ShopListing("chest_store", ShopItemKind.Placeable, null, new Gold(100), BuildingId.GeneralStore, 3, null, false, PlaceableId.Chest, null),
            ],
            data.ShopListings.Skip(Backpack24));
    }

    [Fact]
    public void AC5_The_shipped_shop_file_loads_with_both_backpacks_and_the_chest()
    {
        var ids = TestGames.Shipped.ShopListings.Select(listing => listing.Id).ToList();

        Assert.Contains("backpack_24", ids);
        Assert.Contains("backpack_36", ids);
        Assert.Contains("chest_store", ids);
    }

    // ---- AC5: what is refused ----

    [Fact]
    public void AC5_A_backpack_upgrade_of_capacity_30_is_refused_naming_the_listing()
    {
        var shop = EditListing(Backpack24, "backpackCapacity", 30);

        AssertSingleErrorNaming(LoadErrors(shop), "backpack_24", "backpackCapacity");
    }

    [Fact]
    public void AC5_A_placeable_with_no_placeableId_is_refused_naming_the_listing()
    {
        var shop = EditListing(Chest, "placeableId", null);

        AssertSingleErrorNaming(LoadErrors(shop), "chest_store", "placeableId");
    }

    [Theory]
    [InlineData(12)]
    [InlineData(0)]
    [InlineData(48)]
    public void BR_K1_A_backpack_upgrade_capacity_other_than_24_or_36_is_refused(int capacity)
    {
        var shop = EditListing(Backpack36, "backpackCapacity", capacity);

        AssertSingleErrorNaming(LoadErrors(shop), "backpack_36", "backpackCapacity");
    }

    [Fact]
    public void A_backpack_upgrade_with_no_backpackCapacity_is_refused()
    {
        var shop = EditListing(Backpack24, "backpackCapacity", null);

        AssertSingleErrorNaming(LoadErrors(shop), "backpack_24", "backpackCapacity");
    }

    [Theory]
    [InlineData("cropTypeId", "emberroot")]
    [InlineData("toolId", "axe")]
    [InlineData("placeableId", "chest")]
    public void A_backpack_upgrade_with_a_crop_tool_or_placeable_id_is_refused(string field, string value)
    {
        var shop = EditListing(Backpack24, field, value);

        AssertSingleErrorNaming(LoadErrors(shop), "backpack_24", field);
    }

    [Fact]
    public void BR_L5_A_backpack_upgrade_that_is_not_once_per_game_is_refused()
    {
        var shop = EditListing(Backpack24, "oncePerGame", false);

        AssertSingleErrorNaming(LoadErrors(shop), "backpack_24", "oncePerGame");
    }

    [Theory]
    [InlineData("cropTypeId", "emberroot")]
    [InlineData("toolId", "axe")]
    public void A_placeable_with_a_crop_or_tool_id_is_refused(string field, string value)
    {
        var shop = EditListing(Chest, field, value);

        AssertSingleErrorNaming(LoadErrors(shop), "chest_store", field);
    }

    [Fact]
    public void A_placeable_with_a_backpackCapacity_is_refused()
    {
        var shop = EditListing(Chest, "backpackCapacity", 24);

        AssertSingleErrorNaming(LoadErrors(shop), "chest_store", "backpackCapacity");
    }

    [Theory]
    [InlineData(Seed, "seed_emberroot")]
    [InlineData(Tool, "tool_axe")]
    [InlineData(CanUpgrade, "can_upgrade")]
    public void Another_kind_with_a_placeableId_is_refused(int index, string listingId)
    {
        var shop = EditListing(index, "placeableId", "chest");

        AssertSingleErrorNaming(LoadErrors(shop), listingId, "placeableId");
    }

    [Theory]
    [InlineData(Seed, "seed_emberroot")]
    [InlineData(Tool, "tool_axe")]
    [InlineData(CanUpgrade, "can_upgrade")]
    public void Another_kind_with_a_backpackCapacity_is_refused(int index, string listingId)
    {
        var shop = EditListing(index, "backpackCapacity", 24);

        AssertSingleErrorNaming(LoadErrors(shop), listingId, "backpackCapacity");
    }
}
