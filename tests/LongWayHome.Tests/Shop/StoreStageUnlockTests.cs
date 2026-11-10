using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// #166: the general store's stage 2 and 3 unlocks at Maggie's counter — the 24- and 36-slot backpacks and the chest
/// (BR-B5, BR-L5, BR-L2, BR-K1, BR-K3).
/// </summary>
public class StoreStageUnlockTests
{
    private const string Backpack24 = "backpack_24";

    private const string Backpack36 = "backpack_36";

    private const string ChestStore = "chest_store";

    private static readonly string[] Unlocks = [Backpack24, Backpack36, ChestStore];

    private static readonly ItemRef Chest = ItemRef.Placeable(PlaceableId.Chest);

    private static readonly GridPosition Feet = new(3, 8);

    /// <summary>A Game with the general store at <paramref name="stage"/>, the given Backpack (the tools only) and gold.</summary>
    private static GameState AtStage(int stage, int gold = 100, Backpack? backpack = null) =>
        BuildingGames.WithBuildings(BuildingGames.All(storeStage: stage), backpack, gold);

    private static List<string> OfferIds(GameState game) =>
        ShopService.Offers(game, TestGames.Shipped).Select(listing => listing.Id).ToList();

    private static List<string> UnlockOffers(GameState game) =>
        OfferIds(game).Where(Unlocks.Contains).ToList();

    /// <summary>A Backpack raised to <paramref name="capacity"/>, the tools in Slots 0 and 1.</summary>
    private static Backpack BackpackOf(int capacity)
    {
        var backpack = TestGames.ToolsOnly();
        if (capacity > Backpack.StartingCapacity)
        {
            backpack.RaiseCapacity(capacity);
        }

        return backpack;
    }

    private static (int Gold, string Backpack, int Energy, string Purchased) Snapshot(GameState game) =>
        (game.Player.Gold.Value,
            TestGames.Describe(game.Player.Backpack),
            game.Player.Energy,
            string.Join(",", game.PurchasedListings.Order(StringComparer.Ordinal)));

    // ---- AC1: offers by store stage ----

    [Fact]
    public void AC1_At_store_stage_1_neither_backpack_nor_the_chest_is_offered()
    {
        // The shipped shop lists all three (AC5), so only the stage keeps them off the counter.
        Assert.All(Unlocks, id => Assert.Contains(TestGames.Shipped.ShopListings, listing => listing.Id == id));

        Assert.Empty(UnlockOffers(AtStage(1, gold: 5000)));
    }

    [Fact]
    public void AC1_At_store_stage_2_only_backpack_24_is_offered()
    {
        Assert.Equal([Backpack24], UnlockOffers(AtStage(2, gold: 5000)));
    }

    [Fact]
    public void AC1_At_store_stage_3_backpack_24_backpack_36_and_chest_store_are_offered()
    {
        Assert.Equal([Backpack24, Backpack36, ChestStore], UnlockOffers(AtStage(3, gold: 5000)));
    }

    [Fact]
    public void BR_B5_At_store_stage_4_the_stage_2_and_3_unlocks_are_still_offered()
    {
        Assert.Equal([Backpack24, Backpack36, ChestStore], UnlockOffers(AtStage(4, gold: 5000)));
    }

    [Fact]
    public void BR_L5_Offers_list_seeds_then_tools_then_backpack_upgrades_then_placeables()
    {
        var kinds = ShopService.Offers(AtStage(3, gold: 5000), TestGames.Shipped)
            .Select(listing => listing.Kind)
            .Where(kind => kind != ShopItemKind.CanUpgrade)
            .ToList();

        Assert.Equal(
            [ShopItemKind.Seed, ShopItemKind.Seed, ShopItemKind.Tool, ShopItemKind.Tool, ShopItemKind.BackpackUpgrade, ShopItemKind.BackpackUpgrade, ShopItemKind.Placeable],
            kinds);
    }

    [Fact]
    public void BR_L5_A_backpack_upgrade_above_the_stores_stage_is_NotOffered_and_changes_nothing()
    {
        // The shipped shop lists backpack_36 (AC5), so only the store's stage refuses it.
        Assert.Contains(TestGames.Shipped.ShopListings, listing => listing.Id == Backpack36);
        var game = AtStage(2, gold: 5000);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, Backpack36);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC2: buying the 24-slot backpack ----

    [Fact]
    public void AC2_Buying_backpack_24_with_500_gold_leaves_100_gold_and_a_24_slot_backpack()
    {
        var game = AtStage(2, gold: 500);

        var result = ShopService.Buy(game, TestGames.Shipped, Backpack24);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(100), game.Player.Gold);
        Assert.Equal(24, game.Player.Backpack.Capacity);
    }

    [Fact]
    public void AC2_Buying_backpack_24_keeps_slots_0_to_11_and_adds_empty_slots_12_to_23()
    {
        var game = AtStage(2, gold: 500, backpack: TestGames.DistinctBackpack());
        var before = game.Player.Backpack.Slots.Select(TestGames.DescribeSlot).ToList();

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack24));

        var slots = game.Player.Backpack.Slots.Select(TestGames.DescribeSlot).ToList();
        Assert.Equal(24, slots.Count);
        Assert.Equal(before, slots.Take(12));
        Assert.Equal(Enumerable.Range(12, 12).Select(index => $"{index}:-:-:0:-"), slots.Skip(12));
    }

    [Fact]
    public void AC2_A_bought_backpack_24_is_no_longer_offered()
    {
        var game = AtStage(2, gold: 500);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack24));

        Assert.DoesNotContain(Backpack24, OfferIds(game));
    }

    [Fact]
    public void BR_L5_Buying_backpack_24_records_it_in_the_purchased_listings()
    {
        var game = AtStage(2, gold: 500);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack24));

        Assert.Equal([Backpack24], game.PurchasedListings);
    }

    [Fact]
    public void BR_K1_Buying_backpack_24_keeps_the_hotbar_row()
    {
        var backpack = new Backpack(
            Backpack.StartingCapacity,
            1,
            TestGames.ToolsOnly().Slots);
        var game = AtStage(2, gold: 500, backpack: backpack);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack24));

        Assert.Equal(1, game.Player.Backpack.HotbarRow);
    }

    [Fact]
    public void BR_L2_Buying_backpack_24_with_399_gold_is_NotEnoughGold_and_changes_nothing()
    {
        var game = AtStage(2, gold: 399);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, Backpack24);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, Snapshot(game));
        Assert.Contains(Backpack24, OfferIds(game));
    }

    [Fact]
    public void BR_L2_Exactly_400_gold_buys_backpack_24_and_leaves_0_gold()
    {
        var game = AtStage(2, gold: 400);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack24));

        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal(24, game.Player.Backpack.Capacity);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Buying_a_backpack_upgrade_with_a_quantity_other_than_1_throws_and_changes_nothing(int quantity)
    {
        var game = AtStage(2, gold: 5000);
        var before = Snapshot(game);

        Assert.Throws<ArgumentOutOfRangeException>(() => ShopService.Buy(game, TestGames.Shipped, Backpack24, quantity));
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_P2_Buying_a_backpack_upgrade_with_0_energy_succeeds_and_costs_no_energy()
    {
        var game = TestGames.WithStoreAt(TestGames.WithBackpack(TestGames.ToolsOnly(), gold: 500, energy: 0), stage: 2);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack24));

        Assert.Equal(0, game.Player.Energy);
    }

    // ---- AC3: the 36-slot backpack ----

    [Fact]
    public void AC3_With_a_24_slot_backpack_buying_backpack_36_raises_it_to_36()
    {
        var game = AtStage(3, gold: 1000, backpack: BackpackOf(24));

        var result = ShopService.Buy(game, TestGames.Shipped, Backpack36);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal(36, game.Player.Backpack.Capacity);
    }

    [Fact]
    public void AC3_With_a_36_slot_backpack_neither_backpack_is_offered()
    {
        var game = AtStage(3, gold: 5000, backpack: BackpackOf(36));

        var offers = OfferIds(game);

        Assert.DoesNotContain(Backpack24, offers);
        Assert.DoesNotContain(Backpack36, offers);
        Assert.Contains(ChestStore, offers);
    }

    [Fact]
    public void AC3_Buying_backpack_36_with_a_12_slot_backpack_raises_it_straight_to_36()
    {
        var game = AtStage(3, gold: 1000, backpack: TestGames.DistinctBackpack());
        var before = game.Player.Backpack.Slots.Select(TestGames.DescribeSlot).ToList();

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack36));

        var slots = game.Player.Backpack.Slots.Select(TestGames.DescribeSlot).ToList();
        Assert.Equal(36, slots.Count);
        Assert.Equal(before, slots.Take(12));
        Assert.Equal(Enumerable.Range(12, 24).Select(index => $"{index}:-:-:0:-"), slots.Skip(12));
    }

    [Fact]
    public void BR_K1_After_buying_backpack_36_the_24_listing_is_not_offered_either()
    {
        var game = AtStage(3, gold: 1000);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Backpack36));

        Assert.DoesNotContain(Backpack24, OfferIds(game));
        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), ShopService.Buy(game, TestGames.Shipped, Backpack24));
        Assert.Equal(36, game.Player.Backpack.Capacity);
    }

    [Fact]
    public void BR_K1_With_a_24_slot_backpack_not_bought_here_backpack_24_is_not_offered()
    {
        // A 24-slot Backpack from a migrated save (AC15 of #128): the 24 listing would not raise it.
        var game = AtStage(3, gold: 5000, backpack: BackpackOf(24));

        var offers = OfferIds(game);

        Assert.DoesNotContain(Backpack24, offers);
        Assert.Contains(Backpack36, offers);
    }

    [Fact]
    public void BR_K1_A_full_backpack_does_not_stop_a_backpack_upgrade()
    {
        var game = AtStage(2, gold: 500, backpack: TestGames.FullBackpack());
        var before = game.Player.Backpack.Slots.Select(TestGames.DescribeSlot).ToList();

        var result = ShopService.Buy(game, TestGames.Shipped, Backpack24);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(24, game.Player.Backpack.Capacity);
        Assert.Equal(before, game.Player.Backpack.Slots.Take(12).Select(TestGames.DescribeSlot));
    }

    [Fact]
    public void BR_L2_Buying_backpack_36_with_999_gold_is_NotEnoughGold_and_changes_nothing()
    {
        var game = AtStage(3, gold: 999, backpack: BackpackOf(24));
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, Backpack36);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC4: chests ----

    [Fact]
    public void AC4_Buying_3_chests_takes_300_gold_and_adds_3_chests_to_one_slot()
    {
        var game = AtStage(3, gold: 500);

        var result = ShopService.Buy(game, TestGames.Shipped, ChestStore, 3, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(200), game.Player.Gold);
        var slot = Assert.Single(game.Player.Backpack.Slots, slot => slot.Item == Chest);
        Assert.Equal((2, 3), (slot.Index, slot.Quantity));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void AC4_With_no_room_bought_chests_drop_at_the_feet()
    {
        var game = AtStage(3, gold: 500, backpack: TestGames.FullBackpack());

        var result = ShopService.Buy(game, TestGames.Shipped, ChestStore, 3, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(200), game.Player.Gold);
        Assert.Equal(0, game.Player.Backpack.Quantity(Chest));
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal((Feet, Chest, 3), (dropped.Position, dropped.Item, dropped.Quantity));
    }

    [Fact]
    public void BR_K3_Bought_chests_go_onto_a_chest_slot_below_99_first()
    {
        var backpack = TestGames.BackpackWith((ItemRef.Seed(CropTypeId.Emberroot), 5), (Chest, 2));
        var game = AtStage(3, gold: 500, backpack: backpack);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, ChestStore, 1, Feet));

        var slot = Assert.Single(game.Player.Backpack.Slots, slot => slot.Item == Chest);
        Assert.Equal((3, 3), (slot.Index, slot.Quantity));
    }

    [Fact]
    public void BR_L5_A_bought_chest_is_still_offered_and_not_recorded()
    {
        var game = AtStage(3, gold: 500);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, ChestStore, 1, Feet));

        Assert.Contains(ChestStore, OfferIds(game));
        Assert.Empty(game.PurchasedListings);
    }

    [Fact]
    public void BR_L2_Buying_3_chests_with_299_gold_is_NotEnoughGold_and_changes_nothing()
    {
        var game = AtStage(3, gold: 299);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, ChestStore, 3, Feet);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, Snapshot(game));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_K3_Buying_chests_without_feet_drops_at_plot_tile_0_0()
    {
        var game = AtStage(3, gold: 500, backpack: TestGames.FullBackpack());

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, ChestStore, 2));

        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal((new GridPosition(0, 0), Chest, 2), (dropped.Position, dropped.Item, dropped.Quantity));
    }

    // ---- AC8: the menu's rows and icons ----

    private static ShopListing BackpackListing(int capacity, int price, int stage) =>
        new($"backpack_{capacity}", ShopItemKind.BackpackUpgrade, null, new Gold(price), BuildingId.GeneralStore, stage, null, true, null, capacity);

    private static readonly ShopListing ChestListing =
        new(ChestStore, ShopItemKind.Placeable, null, new Gold(100), BuildingId.GeneralStore, 3, null, false, PlaceableId.Chest, null);

    [Theory]
    [InlineData(24, 400, 2, "Backpack (24 slots) — 400g")]
    [InlineData(36, 1000, 3, "Backpack (36 slots) — 1000g")]
    public void AC8_A_backpack_upgrade_row_shows_its_slots_and_price(int capacity, int price, int stage, string expected)
    {
        Assert.Equal(expected, ShopText.Row(BackpackListing(capacity, price, stage), TestGames.Shipped));
    }

    [Fact]
    public void AC8_The_chest_row_shows_Chest_and_its_price()
    {
        Assert.Equal("Chest — 100g", ShopText.Row(ChestListing, TestGames.Shipped));
    }

    [Fact]
    public void AC8_At_store_stage_2_the_menu_lists_Backpack_24_slots_400g()
    {
        var game = AtStage(2, gold: 500);

        var rows = ShopService.Offers(game, TestGames.Shipped).Select(listing => ShopText.Row(listing, TestGames.Shipped));

        Assert.Contains("Backpack (24 slots) — 400g", rows);
    }

    [Fact]
    public void AC8_A_backpack_upgrade_listing_shows_the_backpack_upgrade_icon()
    {
        Assert.Equal("res://art/icons/backpack_upgrade.svg", ItemIcons.For(BackpackListing(24, 400, 2)));
    }

    [Fact]
    public void AC8_The_chest_listing_shows_the_placeable_chest_icon()
    {
        Assert.Equal(ItemIcons.PlaceableIcon(PlaceableId.Chest), ItemIcons.For(ChestListing));
    }

    [Fact]
    public void AC8_The_backpack_upgrade_icon_file_exists()
    {
        var path = Path.Combine(LongWayHome.Tests.Game.SceneText.RepoRoot(), "game", "art", "icons", "backpack_upgrade.svg");

        Assert.True(File.Exists(path), "game/art/icons/backpack_upgrade.svg not found");
    }
}
