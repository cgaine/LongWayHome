using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// The seeds shop (#45) and the Watering can upgrade (#46): BR-L1, BR-L2, BR-P2, BR-P5, BR-P7, BR-W1, BR-W3, BR-W4, and
/// GameSession.Buy's events.
/// </summary>
public class ShopTests
{
    private static GameDate SeasonDay(Season season) => new(1, season, 1);

    private static string[] OfferIds(GameState game) =>
        ShopService.Offers(game, TestGames.Shipped).Select(listing => listing.Id).ToArray();

    /// <summary>Gold, energy, every Tile and every inventory stack, to prove a purchase changed nothing.</summary>
    private static (int Gold, (int Energy, string Tiles, string Inventory) State) Snapshot(GameState game) =>
        (game.Player.Gold.Value, TestGames.FullSnapshot(game));

    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.PlayerChanged += () => events.Add("PlayerChanged");
        session.TileChanged += position => events.Add($"TileChanged:{position}");
        return events;
    }

    [Fact]
    public void BR_L1_spring_shop_offers_only_emberroot_and_dusktuber_seeds()
    {
        var game = TestGames.WithGold(100, TestGames.FirstDay);

        // #46 appends the Can upgrade after the seeds; this test is about the seeds.
        var seedOffers = ShopService.Offers(game, TestGames.Shipped)
            .Where(listing => listing.Kind == ShopItemKind.Seed)
            .Select(listing => listing.Id);

        Assert.Equal(["seed_emberroot", "seed_dusktuber"], seedOffers);
    }

    [Theory]
    [InlineData(Season.Summer, new[] { "seed_sunbell", "seed_starcorn" })]
    [InlineData(Season.Fall, new[] { "seed_moonmelon" })]
    [InlineData(Season.Winter, new string[0])]
    public void AC2_BR_L1_shop_offers_the_seeds_of_the_current_season_in_listing_order(Season season, string[] expected)
    {
        var game = TestGames.WithGold(100, SeasonDay(season));

        // #46 appends the Can upgrade after the seeds; this test is about the seeds.
        var seedOffers = ShopService.Offers(game, TestGames.Shipped)
            .Where(listing => listing.Kind == ShopItemKind.Seed)
            .Select(listing => listing.Id);

        Assert.Equal(expected, seedOffers);
    }

    [Fact]
    public void BR_L1_a_seed_listing_whose_crop_type_has_no_data_row_is_not_offered()
    {
        var game = TestGames.WithGold(100, TestGames.FirstDay);
        var data = TestGames.ShippedWithout(CropTypeId.Emberroot);

        // #46 appends the Can upgrade after the seeds; this test is about the seeds.
        var offers = ShopService.Offers(game, data)
            .Where(listing => listing.Kind == ShopItemKind.Seed)
            .Select(listing => listing.Id);

        Assert.Equal(["seed_dusktuber"], offers);
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Summer)]
    [InlineData(Season.Fall)]
    [InlineData(Season.Winter)]
    public void BR_L1_the_can_upgrade_is_offered_in_every_season_last_while_not_upgraded(Season season)
    {
        var game = TestGames.WithGold(1000, SeasonDay(season));

        var offers = ShopService.Offers(game, TestGames.Shipped);

        Assert.NotEmpty(offers);
        Assert.Equal("can_upgrade", offers[^1].Id);
        Assert.Equal(ShopItemKind.CanUpgrade, offers[^1].Kind);
        Assert.Single(offers, listing => listing.Kind == ShopItemKind.CanUpgrade);
        // #145: the axe and pickaxe come between the seeds and the Can upgrade.
        Assert.All(offers.SkipLast(1), listing => Assert.Contains(listing.Kind, new[] { ShopItemKind.Seed, ShopItemKind.Tool }));
    }

    [Fact]
    public void AC3_BR_L2_buying_emberroot_in_a_new_game_takes_15_gold_and_adds_one_seed()
    {
        // BR-P6 (#139): a new Game holding the starter Seeds Maggie gives.
        var game = TestGames.NewGameWithSeeds();

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(85), game.Player.Gold);
        Assert.Single(TestGames.Stacks(game.Player.Backpack), stack => stack.Kind == ItemKind.Seed && stack.CropTypeId == CropTypeId.Emberroot);
        Assert.Equal(11, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
    }

    /// <summary>Tools, 90 emberroot seeds in Slot 2, every other Slot full: room for exactly 9 more emberroot seeds.</summary>
    private static Backpack RoomForNineEmberrootSeeds() =>
        TestGames.FullBackpack((ItemRef.Seed(CropTypeId.Emberroot), 90));

    // #130 changed the next tests: a purchase is never refused for room; what doesn't fit drops at the farmer's feet.
    private static readonly GridPosition Feet = new(3, 8);

    [Fact]
    public void BR_K3_Buying_seeds_that_do_not_all_fit_takes_the_gold_and_drops_the_rest()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 10, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(1000 - (10 * 15)), game.Player.Gold);
        Assert.Equal("2:Seed:Emberroot:99:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal((Feet, ItemRef.Seed(CropTypeId.Emberroot), 1), (dropped.Position, dropped.Item, dropped.Quantity));
    }

    [Fact]
    public void BR_K3_Buying_a_seed_type_with_no_free_slot_drops_it_all()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", 1, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(1000 - 30), game.Player.Gold);
        Assert.Equal(0, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal((Feet, ItemRef.Seed(CropTypeId.Dusktuber), 1), (dropped.Position, dropped.Item, dropped.Quantity));
    }

    [Fact]
    public void BR_K3_Buying_without_feet_drops_at_plot_tile_0_0()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 1000);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new GridPosition(0, 0), Assert.Single(game.DroppedItems).Position);
    }

    [Fact]
    public void BR_L2_Session_buy_of_seeds_that_do_not_fit_raises_player_and_inventory_changed()
    {
        var session = new GameSession(TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.Buy("seed_emberroot", 10, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Contains("PlayerChanged", events);
        Assert.Contains("InventoryChanged", events);
        Assert.Single(session.Game.DroppedItems);
    }

    [Fact]
    public void BR_K3_Seeds_bought_in_town_that_do_not_fit_drop_in_town()
    {
        var session = new GameSession(TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));

        var result = session.Buy("seed_emberroot", 10, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(AreaId.Town, Assert.Single(session.Game.DroppedItems).Area);
    }

    [Fact]
    public void BR_L2_full_backpack_purchase_is_not_refused()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 100);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 2, Feet);

        // BR-L2: the gold is taken and the seeds gained (here, all dropped) in the same step.
        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(100 - (2 * 15)), game.Player.Gold);
        Assert.Equal(2, Assert.Single(game.DroppedItems).Quantity);
    }

    [Fact]
    public void BR_L2_Buying_seeds_that_just_fit_takes_the_gold_and_adds_them_in_the_same_step()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 9);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(1000 - (9 * 15)), game.Player.Gold);
        Assert.Equal("2:Seed:Emberroot:99:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
    }

    [Fact]
    public void BR_P5_Exactly_enough_gold_and_room_for_only_part_spends_it_all_and_drops_the_rest()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 10 * 15);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 10, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal(1, Assert.Single(game.DroppedItems).Quantity);
    }

    [Fact]
    public void BR_L2_Bought_seeds_go_into_the_backpack()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack());

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", 2);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(100 - (2 * 30)), game.Player.Gold);
        Assert.Equal("3:Seed:Dusktuber:7:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[3]));
    }

    [Fact]
    public void BR_P7_bought_seeds_merge_into_the_existing_stack()
    {
        var game = TestGames.WithGold(100);

        ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber");

        Assert.Equal(
            [
                (ItemKind.Seed, CropTypeId.Emberroot, 10),
                (ItemKind.Seed, CropTypeId.Dusktuber, 6),
            ],
            TestGames.Stacks(game.Player.Backpack));
    }

    [Fact]
    public void BR_P7_buying_a_seed_type_not_held_creates_a_new_stack_that_becomes_holdable()
    {
        var session = new GameSession(TestGames.WithGold(100, SeasonDay(Season.Summer)), TestGames.Shipped);
        Assert.DoesNotContain(HeldItem.SeedOf(CropTypeId.Sunbell), TestGames.HeldItemsInHotbar(session));

        var result = session.Buy("seed_sunbell");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Sunbell)));
        Assert.Contains(HeldItem.SeedOf(CropTypeId.Sunbell), TestGames.HeldItemsInHotbar(session));
    }

    [Fact]
    public void BR_L2_buying_without_enough_gold_changes_nothing()
    {
        var game = TestGames.WithGold(10);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber");

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_L2_buying_several_without_enough_gold_for_all_of_them_changes_nothing()
    {
        var game = TestGames.WithGold(119);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", 4);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC5_BR_P5_exactly_enough_gold_buys_and_leaves_0_gold()
    {
        var game = TestGames.WithGold(30);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal(6, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void BR_P5_buying_with_0_gold_is_refused_and_gold_stays_at_0()
    {
        var game = TestGames.WithGold(0);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(new Gold(0), game.Player.Gold);
    }

    [Theory]
    [InlineData("seed_moonmelon")]
    [InlineData("seed_sunbell")]
    [InlineData("no_such_listing")]
    public void AC6_BR_L1_buying_a_listing_not_offered_this_season_is_not_offered_and_changes_nothing(string listingId)
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, listingId);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC6_not_offered_is_reported_before_not_enough_gold()
    {
        var game = TestGames.WithGold(0, TestGames.FirstDay);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_moonmelon");

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
    }

    [Fact]
    public void AC7_buying_4_dusktuber_with_415_gold_leaves_295_and_adds_4_seeds()
    {
        var game = TestGames.WithGold(415);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", 4);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(295), game.Player.Gold);
        Assert.Equal(9, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AC7_buying_a_quantity_below_1_throws_and_changes_nothing(int quantity)
    {
        var game = TestGames.WithGold(415);
        var before = Snapshot(game);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", quantity));
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC7_a_total_price_past_int_max_throws_overflow_and_changes_nothing()
    {
        var game = TestGames.WithGold(int.MaxValue);
        var before = Snapshot(game);

        Assert.Throws<OverflowException>(
            () => ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", int.MaxValue));
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_L2_a_seed_past_what_the_backpack_holds_takes_the_gold_and_drops_at_the_feet()
    {
        // BR-K2: a Slot holds at most 99, so every Slot full of emberroot seeds is as many as can be carried.
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Emberroot), 10 * Backpack.MaxStack);
        var game = TestGames.WithGold(100, TestGames.FirstDay, inventory);
        var before = TestGames.Describe(game.Player.Backpack);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 1, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(85), game.Player.Gold);
        Assert.Equal(before, TestGames.Describe(game.Player.Backpack));
        Assert.Equal(1, Assert.Single(game.DroppedItems).Quantity);
    }

    [Fact]
    public void BR_P2_buying_spends_no_energy()
    {
        var game = TestGames.WithGold(100);
        var energy = game.Player.Energy;

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(energy, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_buying_with_0_energy_still_succeeds()
    {
        var game = TestGames.WithEnergy(0);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(0, game.Player.Energy);
        Assert.Equal(new Gold(85), game.Player.Gold);
    }

    [Fact]
    public void AC8_session_buy_raises_player_and_inventory_changed_on_success()
    {
        var session = TestGames.NewSession();
        var events = RecordEvents(session);

        var result = session.Buy("seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(["InventoryChanged", "PlayerChanged"], events.Order());
        Assert.Equal(new Gold(85), session.Game.Player.Gold);
        Assert.Equal(11, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
    }

    [Fact]
    public void AC8_session_buy_raises_nothing_when_not_enough_gold()
    {
        var session = new GameSession(TestGames.WithGold(10), TestGames.Shipped);
        var events = RecordEvents(session);
        var before = Snapshot(session.Game);

        var result = session.Buy("seed_dusktuber");

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Empty(events);
        Assert.Equal(before, Snapshot(session.Game));
    }

    [Fact]
    public void AC8_session_buy_raises_nothing_when_not_offered()
    {
        var session = TestGames.NewSession();
        var events = RecordEvents(session);

        var result = session.Buy("seed_moonmelon");

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Empty(events);
    }

    [Fact]
    public void AC8_session_buy_passes_the_quantity_through()
    {
        var session = new GameSession(TestGames.WithGold(415), TestGames.Shipped);

        var result = session.Buy("seed_dusktuber", 4);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(295), session.Game.Player.Gold);
        Assert.Equal(9, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Summer)]
    [InlineData(Season.Fall)]
    [InlineData(Season.Winter)]
    public void AC8_session_shop_offers_equal_the_shop_service_offers(Season season)
    {
        var session = new GameSession(TestGames.WithGold(100, SeasonDay(season)), TestGames.Shipped);

        Assert.Equal(ShopService.Offers(session.Game, session.Data), session.ShopOffers());
    }

    // ---- #46: the Watering can upgrade ----

    private const string CanUpgrade = "can_upgrade";

    private const string Axe = "tool_axe";

    private const string Pickaxe = "tool_pickaxe";

    private static int UpgradePrice => TestGames.Shipped.ShopListings.Single(listing => listing.Id == CanUpgrade).Price.Value;

    private static int UpgradedCapacity => TestGames.Shipped.Tuning.CanUpgradedCapacity;

    private static (int Capacity, int Charges, bool Upgraded) CanState(GameState game) =>
        (game.Player.WateringCan.Capacity, game.Player.WateringCan.Charges, game.Player.WateringCan.Upgraded);

    /// <summary>A can already upgraded to the shipped upgraded capacity, holding the given charges.</summary>
    private static WateringCan UpgradedCan(int charges) => new(UpgradedCapacity, charges, true);

    [Fact]
    public void Shipped_data_prices_the_can_upgrade_at_200_and_upgrades_to_40()
    {
        Assert.Equal(200, UpgradePrice);
        Assert.Equal(40, UpgradedCapacity);
    }

    [Fact]
    public void AC1_spring_offers_emberroot_dusktuber_then_the_can_upgrade()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        // #145: the axe and pickaxe come between the seeds and the Can upgrade.
        Assert.Equal(["seed_emberroot", "seed_dusktuber", Axe, Pickaxe, CanUpgrade], OfferIds(game));
    }

    [Fact]
    public void AC1_winter_offers_the_tools_and_the_can_upgrade_alone()
    {
        var game = TestGames.WithGold(1000, SeasonDay(Season.Winter));

        // #145: no seeds in winter; the axe and pickaxe are offered in every Season.
        Assert.Equal([Axe, Pickaxe, CanUpgrade], OfferIds(game));
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Summer)]
    [InlineData(Season.Fall)]
    [InlineData(Season.Winter)]
    public void BR_L1_seed_offers_are_unchanged_and_come_before_the_can_upgrade(Season season)
    {
        var game = TestGames.WithGold(1000, SeasonDay(season));
        var seeds = TestGames.Shipped.ShopListings
            .Where(listing => listing.Kind == ShopItemKind.Seed
                && TestGames.Shipped.CropTypes[listing.CropTypeId!.Value].Season == season)
            .Select(listing => listing.Id);

        Assert.Equal([.. seeds, Axe, Pickaxe, CanUpgrade], OfferIds(game));
    }

    [Fact]
    public void BR_L1_the_can_upgrade_is_not_offered_once_upgraded()
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay, can: UpgradedCan(40));

        Assert.Equal(["seed_emberroot", "seed_dusktuber", Axe, Pickaxe], OfferIds(game));
    }

    [Fact]
    public void AC11_Buying_the_can_upgrade_sets_the_cans_tier_to_rosk_and_its_capacity_to_40()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack(), gold: 500, can: new WateringCan(20, 5, false));

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(ToolTier.Rosk, game.Player.Backpack.TierOf(ToolId.WateringCan));
        Assert.Equal("1:Tool:WateringCan:1:Rosk", TestGames.DescribeSlot(game.Player.Backpack.Slots[1]));
        Assert.Equal(40, game.Player.WateringCan.Capacity);
    }

    [Fact]
    public void BR_W1_After_the_can_upgrade_the_rosk_can_holds_40_charges_and_the_hoe_stays_basic()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack(), gold: 500, can: new WateringCan(20, 5, false));

        ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal((40, 40, true), CanState(game));
        Assert.Equal(ToolTier.Basic, game.Player.Backpack.TierOf(ToolId.Hoe));
    }

    [Fact]
    public void AC11_A_can_upgrade_refused_for_gold_leaves_the_can_basic()
    {
        var game = TestGames.WithBackpack(TestGames.StartingBackpack(), gold: 199);

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(ToolTier.Basic, game.Player.Backpack.TierOf(ToolId.WateringCan));
    }

    [Fact]
    public void AC2_BR_W4_buying_the_upgrade_with_200_gold_leaves_0_gold_and_a_full_40_charge_upgraded_can()
    {
        var game = TestGames.WithGold(200);

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal((40, 40, true), CanState(game));
    }

    [Fact]
    public void AC3_BR_W4_buying_the_upgrade_with_the_can_at_5_of_20_leaves_it_at_40_of_40()
    {
        var game = TestGames.WithGold(500, can: new WateringCan(20, 5, false));

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal((40, 40, true), CanState(game));
    }

    [Fact]
    public void AC4_after_buying_the_upgrade_it_is_no_longer_offered_and_buying_again_changes_nothing()
    {
        var game = TestGames.WithGold(1000);
        ShopService.Buy(game, TestGames.Shipped, CanUpgrade);
        game.Player.WateringCan.TryUseCharge();
        var before = (Snapshot(game), CanState(game));

        var offers = OfferIds(game);
        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.DoesNotContain(CanUpgrade, offers);
        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, (Snapshot(game), CanState(game)));
        Assert.Equal(new Gold(1000 - UpgradePrice), game.Player.Gold);
    }

    [Fact]
    public void AC4_BR_W4_buying_the_upgrade_for_an_already_upgraded_can_is_not_offered_and_changes_nothing()
    {
        var game = TestGames.WithGold(1000, can: UpgradedCan(12));
        var before = (Snapshot(game), CanState(game));

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, (Snapshot(game), CanState(game)));
    }

    [Fact]
    public void AC5_BR_L2_buying_the_upgrade_with_199_gold_is_not_enough_gold_and_changes_nothing()
    {
        var game = TestGames.WithGold(199, can: new WateringCan(20, 5, false));
        var before = (Snapshot(game), CanState(game));

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, (Snapshot(game), CanState(game)));
        Assert.Equal((20, 5, false), CanState(game));
    }

    [Fact]
    public void BR_L2_buying_the_upgrade_takes_the_listing_price_and_upgrades_in_the_same_call()
    {
        var game = TestGames.WithGold(UpgradePrice + 37);

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(37), game.Player.Gold);
        Assert.Equal((UpgradedCapacity, UpgradedCapacity, true), CanState(game));
    }

    [Fact]
    public void BR_L2_buying_the_upgrade_adds_nothing_to_the_inventory()
    {
        var game = TestGames.WithGold(1000);
        var inventory = TestGames.Stacks(game.Player.Backpack);

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(inventory, TestGames.Stacks(game.Player.Backpack));
    }

    [Fact]
    public void BR_P5_buying_the_upgrade_with_0_gold_is_refused_and_gold_stays_at_0()
    {
        var game = TestGames.WithGold(0);

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.False(game.Player.WateringCan.Upgraded);
    }

    [Fact]
    public void BR_P5_buying_the_upgrade_with_exactly_its_price_leaves_0_gold()
    {
        var game = TestGames.WithGold(UpgradePrice);

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.True(game.Player.WateringCan.Upgraded);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Buying_the_upgrade_in_a_quantity_other_than_1_throws_and_changes_nothing(int quantity)
    {
        var game = TestGames.WithGold(1000);
        var before = (Snapshot(game), CanState(game));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ShopService.Buy(game, TestGames.Shipped, CanUpgrade, quantity));
        Assert.Equal(before, (Snapshot(game), CanState(game)));
    }

    [Fact]
    public void AC6_BR_W1_an_upgraded_can_waters_40_dry_tilled_tiles_and_the_41st_is_can_empty()
    {
        var session = new GameSession(TestGames.WithGold(1000), TestGames.Shipped);
        var positions = session.Game.FarmPlot.Tiles.Select(tile => tile.Position).Take(41).ToList();
        positions.ForEach(position => session.Game.FarmPlot[position].Till());
        Assert.Equal(ActionResult.Ok, session.Buy(CanUpgrade));

        var results = positions.Select(session.Water).ToList();

        Assert.All(results.Take(40), result => Assert.Equal(ActionResult.Ok, result));
        Assert.Equal(ActionResult.Rejected(Rejection.CanEmpty), results[40]);
        Assert.Equal(0, session.Game.Player.WateringCan.Charges);
        Assert.True(session.Game.Player.Energy >= TestGames.Shipped.Tuning.ToolEnergyCost);
    }

    [Fact]
    public void AC6_BR_W3_refilling_an_upgraded_can_at_the_pond_sets_charges_to_40()
    {
        var session = new GameSession(TestGames.WithGold(1000), TestGames.Shipped);
        var positions = session.Game.FarmPlot.Tiles.Select(tile => tile.Position).Take(41).ToList();
        positions.ForEach(position => session.Game.FarmPlot[position].Till());
        session.Buy(CanUpgrade);
        positions.ForEach(position => session.Water(position));
        var energy = session.Game.Player.Energy;

        var result = session.RefillCan(WaterSource.Pond);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal((40, 40, true), CanState(session.Game));
        Assert.Equal(energy, session.Game.Player.Energy);
    }

    [Fact]
    public void AC8_BR_P2_buying_the_upgrade_changes_no_energy()
    {
        var game = TestGames.WithGold(1000);
        var energy = game.Player.Energy;

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(energy, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_buying_the_upgrade_with_0_energy_still_succeeds()
    {
        var game = TestGames.WithGold(1000);
        Assert.True(game.Player.TrySpendEnergy(game.Player.Energy));

        var result = ShopService.Buy(game, TestGames.Shipped, CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(0, game.Player.Energy);
        Assert.True(game.Player.WateringCan.Upgraded);
    }

    [Fact]
    public void Session_buy_of_the_upgrade_raises_player_and_inventory_changed()
    {
        var session = new GameSession(TestGames.WithGold(1000), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.Buy(CanUpgrade);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(["InventoryChanged", "PlayerChanged"], events.Order());
        Assert.Equal((40, 40, true), CanState(session.Game));
    }

    [Fact]
    public void Session_buy_of_the_upgrade_raises_nothing_when_rejected()
    {
        var session = new GameSession(TestGames.WithGold(199), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.Buy(CanUpgrade);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Empty(events);
    }

    [Fact]
    public void Session_shop_offers_drop_the_upgrade_once_bought()
    {
        var session = new GameSession(TestGames.WithGold(1000), TestGames.Shipped);
        Assert.Contains(session.ShopOffers(), listing => listing.Id == CanUpgrade);

        session.Buy(CanUpgrade);

        Assert.DoesNotContain(session.ShopOffers(), listing => listing.Id == CanUpgrade);
    }

    [Fact]
    public void Buying_seeds_is_unchanged_while_the_upgrade_is_offered()
    {
        var game = TestGames.WithGold(100);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(85), game.Player.Gold);
        Assert.Equal(11, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal((20, 20, false), CanState(game));
    }

    [Fact]
    public void BR_L1_the_can_upgrade_comes_after_the_seeds_even_when_listed_first_in_the_data()
    {
        var game = TestGames.WithGold(100, TestGames.FirstDay);
        var listings = TestGames.Shipped.ShopListings;
        var data = TestGames.Shipped with
        {
            ShopListings = listings.Where(listing => listing.Kind == ShopItemKind.CanUpgrade)
                .Concat(listings.Where(listing => listing.Kind != ShopItemKind.CanUpgrade))
                .ToList(),
        };

        var offers = ShopService.Offers(game, data).Select(listing => listing.Id);

        Assert.Equal(["seed_emberroot", "seed_dusktuber", Axe, Pickaxe, CanUpgrade], offers);
    }

    [Fact]
    public void BR_W4_buying_the_upgrade_takes_its_capacity_from_tuning()
    {
        var game = TestGames.WithGold(1000);
        var data = TestGames.Shipped with
        {
            Tuning = TestGames.Shipped.Tuning with { CanUpgradedCapacity = 55 },
        };

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, data, CanUpgrade));
        Assert.Equal((55, 55, true), CanState(game));
    }

    [Fact]
    public void BR_L2_an_upgrade_that_cannot_raise_the_capacity_throws_before_taking_gold()
    {
        var game = TestGames.WithGold(1000, can: new WateringCan(40, 10, false));
        var before = (Snapshot(game), CanState(game));

        Assert.Throws<ArgumentOutOfRangeException>(() => ShopService.Buy(game, TestGames.Shipped, CanUpgrade));
        Assert.Equal(before, (Snapshot(game), CanState(game)));
    }

    // ---- #145: Maggie's counter — the axe, the pickaxe, vendor stages and once-per-game listings ----

    /// <summary>The tools in Slots 0 and 1, 10 emberroot seeds in Slot 3, and Slot 2 empty: the lowest free Slot is 2.</summary>
    private static Backpack GapAtSlot2() =>
        TestGames.RestoredBackpack(new BackpackSlot(3, ItemRef.Seed(CropTypeId.Emberroot), 10, null));

    /// <summary>The same Game rebuilt through the constructor with the given purchased listings.</summary>
    private static GameState WithPurchases(GameState game, params string[] purchased) =>
        new(
            game.Id,
            game.Date,
            game.ClockMinutes,
            game.Weather,
            game.LastDayEnd,
            game.CreatedAt,
            game.SavedAt,
            game.Player,
            game.FarmPlot,
            game.ShippingBin,
            game.Residents,
            game.OpeningDone,
            new HashSet<string>(purchased));

    /// <summary>Gold, the Backpack and the purchased listings, to prove a tool purchase changed nothing.</summary>
    private static (int Gold, string Backpack, string Purchased) ToolState(GameState game) =>
        (game.Player.Gold.Value,
            TestGames.Describe(game.Player.Backpack),
            string.Join(",", game.PurchasedListings.Order(StringComparer.Ordinal)));

    [Fact]
    public void AC1_Spring_1_of_a_new_game_offers_emberroot_dusktuber_the_axe_the_pickaxe_then_the_can_upgrade()
    {
        var session = TestGames.NewSession();

        Assert.Equal(
            ["seed_emberroot", "seed_dusktuber", Axe, Pickaxe, CanUpgrade],
            session.ShopOffers().Select(listing => listing.Id));
    }

    [Fact]
    public void AC1_In_summer_the_seeds_offered_are_sunbell_and_starcorn_before_the_tools()
    {
        var game = TestGames.WithGold(100, SeasonDay(Season.Summer));

        Assert.Equal(["seed_sunbell", "seed_starcorn", Axe, Pickaxe, CanUpgrade], OfferIds(game));
    }

    [Fact]
    public void AC2_Buying_the_axe_with_100_gold_leaves_50_and_the_axe_basic_in_the_lowest_free_slot()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 100);

        var result = ShopService.Buy(game, TestGames.Shipped, Axe);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(50), game.Player.Gold);
        Assert.Equal("2:Tool:Axe:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
    }

    [Fact]
    public void AC2_A_bought_axe_is_no_longer_offered()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 100);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));

        Assert.DoesNotContain(Axe, OfferIds(game));
        Assert.Contains(Pickaxe, OfferIds(game));
    }

    [Fact]
    public void AC2_Buying_the_axe_records_tool_axe_in_the_purchased_listings()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 100);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));

        Assert.Equal([Axe], game.PurchasedListings);
    }

    [Fact]
    public void AC3_Buying_the_pickaxe_with_74_gold_is_NotEnoughGold_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 74);
        var before = ToolState(game);

        var result = ShopService.Buy(game, TestGames.Shipped, Pickaxe);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(before, ToolState(game));
        Assert.Contains(Pickaxe, OfferIds(game));
    }

    [Fact]
    public void AC4_Buying_the_axe_with_every_slot_full_is_BackpackFull_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 1000);
        var before = ToolState(game);

        var result = ShopService.Buy(game, TestGames.Shipped, Axe);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(before, ToolState(game));
    }

    [Fact]
    public void AC4_An_axe_refused_for_a_full_backpack_is_still_offered()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 1000);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), ShopService.Buy(game, TestGames.Shipped, Axe));

        Assert.Contains(Axe, OfferIds(game));
    }

    [Fact]
    public void AC4_Not_enough_gold_is_reported_before_backpack_full()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 10);
        var before = ToolState(game);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), ShopService.Buy(game, TestGames.Shipped, Axe));
        Assert.Equal(before, ToolState(game));
    }

    [Fact]
    public void AC5_Buying_the_axe_a_second_time_is_NotOffered_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 500);
        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));
        var before = ToolState(game);

        var result = ShopService.Buy(game, TestGames.Shipped, Axe);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, ToolState(game));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void AC5_Buying_a_tool_in_a_quantity_other_than_1_throws_and_changes_nothing(int quantity)
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 500);
        var before = ToolState(game);

        Assert.Throws<ArgumentOutOfRangeException>(() => ShopService.Buy(game, TestGames.Shipped, Axe, quantity));
        Assert.Equal(before, ToolState(game));
    }

    /// <summary>
    /// The shipped data plus a general store listing needing stage 2, a carpenter listing needing stage 1 and a
    /// carpenter listing needing stage 0 (spring seeds, so the in-season rule alone would offer them).
    /// </summary>
    private static Application.Data.GameData WithStagedListings() => TestGames.Shipped with
    {
        ShopListings =
        [
            .. TestGames.Shipped.ShopListings,
            new ShopListing("store_stage_2", ShopItemKind.Seed, CropTypeId.Emberroot, new Gold(15), BuildingId.GeneralStore, 2, null, false),
            new ShopListing("carpenter_stage_1", ShopItemKind.Seed, CropTypeId.Dusktuber, new Gold(30), BuildingId.Carpenter, 1, null, false),
            new ShopListing("carpenter_stage_0", ShopItemKind.Seed, CropTypeId.Dusktuber, new Gold(30), BuildingId.Carpenter, 0, null, false),
        ],
    };

    [Fact]
    public void AC6_A_general_store_listing_needing_stage_2_is_not_offered()
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay);

        Assert.DoesNotContain("store_stage_2", ShopService.Offers(game, WithStagedListings()).Select(listing => listing.Id));
    }

    [Fact]
    public void AC6_A_carpenter_listing_needing_stage_1_is_not_offered()
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay);

        Assert.DoesNotContain("carpenter_stage_1", ShopService.Offers(game, WithStagedListings()).Select(listing => listing.Id));
    }

    [Theory]
    [InlineData("store_stage_2")]
    [InlineData("carpenter_stage_1")]
    public void AC6_Buying_a_listing_above_its_vendors_stage_is_NotOffered_and_changes_nothing(string listingId)
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay);
        var before = ToolState(game);

        var result = ShopService.Buy(game, WithStagedListings(), listingId);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, ToolState(game));
    }

    [Fact]
    public void BR_L5_A_listing_at_its_vendors_stage_is_offered()
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay);

        var offers = ShopService.Offers(game, WithStagedListings()).Select(listing => listing.Id).ToList();

        Assert.Contains("carpenter_stage_0", offers);
        Assert.Contains("seed_emberroot", offers);
    }

    [Theory]
    [InlineData(BuildingId.GeneralStore, 1)]
    [InlineData(BuildingId.Carpenter, 0)]
    [InlineData(BuildingId.MirasHouse, 0)]
    [InlineData(BuildingId.Blacksmith, 0)]
    public void BR_L5_VendorStage_is_1_for_the_general_store_and_0_for_every_other_building(BuildingId vendor, int stage)
    {
        var game = TestGames.WithGold(100);

        Assert.Equal(stage, ShopService.VendorStage(game, vendor));
    }

    [Fact]
    public void BR_L5_A_once_per_game_listing_already_purchased_is_not_offered()
    {
        // The pickaxe is not in the Backpack, so only the purchase record keeps it off the shelf.
        var game = WithPurchases(TestGames.WithGold(1000), Pickaxe);

        Assert.DoesNotContain(Pickaxe, OfferIds(game));
        Assert.Contains(Axe, OfferIds(game));
    }

    [Fact]
    public void BR_L5_Buying_the_can_upgrade_records_can_upgrade_in_the_purchased_listings()
    {
        var game = TestGames.WithGold(1000);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, CanUpgrade));

        Assert.Equal([CanUpgrade], game.PurchasedListings);
    }

    [Fact]
    public void BR_L5_Buying_seeds_records_no_purchase()
    {
        // Characterization: seed listings are not once-per-game, so nothing is recorded (passes before #145 too).
        var game = TestGames.WithGold(1000);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 3));

        Assert.Empty(game.PurchasedListings);
    }

    [Fact]
    public void BR_L4_A_tool_already_in_the_backpack_is_not_offered()
    {
        var backpack = TestGames.RestoredBackpack(new BackpackSlot(5, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic));
        var game = TestGames.WithBackpack(backpack, gold: 1000);

        Assert.DoesNotContain(Axe, OfferIds(game));
        Assert.Contains(Pickaxe, OfferIds(game));
    }

    [Fact]
    public void BR_L4_Buying_the_pickaxe_with_75_gold_adds_it_at_basic_and_records_it()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 75);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Pickaxe));

        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal("2:Tool:Pickaxe:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Equal([Pickaxe], game.PurchasedListings);
        Assert.DoesNotContain(Pickaxe, OfferIds(game));
    }

    [Fact]
    public void BR_L4_Buying_both_tools_puts_them_in_successive_free_slots()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 125);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));
        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Pickaxe));

        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal("2:Tool:Axe:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Equal("4:Tool:Pickaxe:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[4]));
        Assert.Equal([Axe, Pickaxe], game.PurchasedListings.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void BR_L2_Buying_the_axe_with_49_gold_is_NotEnoughGold_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 49);
        var before = ToolState(game);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), ShopService.Buy(game, TestGames.Shipped, Axe));
        Assert.Equal(before, ToolState(game));
    }

    [Fact]
    public void BR_P5_Exactly_50_gold_buys_the_axe_and_leaves_0_gold()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 50);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));

        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal("2:Tool:Axe:1:Basic", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
    }

    [Fact]
    public void BR_K2_The_bought_axe_fills_one_slot_with_exactly_one_tool_and_its_tier()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 100);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));

        var slot = Assert.Single(game.Player.Backpack.Slots, slot => slot.Item == ItemRef.Tool(ToolId.Axe));
        Assert.Equal((1, (ToolTier?)ToolTier.Basic), (slot.Quantity, slot.ToolTier));
    }

    [Fact]
    public void BR_P2_Buying_the_axe_costs_no_energy()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 100, energy: 37);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Axe));

        Assert.Equal(37, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_Buying_the_pickaxe_with_0_energy_still_succeeds()
    {
        var game = TestGames.WithBackpack(GapAtSlot2(), gold: 100, energy: 0);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, Pickaxe));

        Assert.Equal(0, game.Player.Energy);
        Assert.Equal(new Gold(25), game.Player.Gold);
    }

    [Fact]
    public void BR_L1_Seed_offers_follow_the_season_and_the_tools_come_after_them()
    {
        // BR-L1 still governs the seeds; the tools are offered in every Season.
        var winter = TestGames.WithGold(1000, SeasonDay(Season.Winter));
        var fall = TestGames.WithGold(1000, SeasonDay(Season.Fall));

        Assert.Equal([Axe, Pickaxe, CanUpgrade], OfferIds(winter));
        Assert.Equal(["seed_moonmelon", Axe, Pickaxe, CanUpgrade], OfferIds(fall));
    }

    [Fact]
    public void BR_L4_Session_buy_of_the_axe_raises_player_and_inventory_changed_and_drops_its_row()
    {
        var session = new GameSession(TestGames.WithBackpack(GapAtSlot2(), gold: 100), TestGames.Shipped);
        var events = RecordEvents(session);

        Assert.Equal(ActionResult.Ok, session.Buy(Axe));

        Assert.Equal(["InventoryChanged", "PlayerChanged"], events.Order());
        Assert.DoesNotContain(session.ShopOffers(), listing => listing.Id == Axe);
    }

    [Fact]
    public void AC12_Buying_a_seed_costs_and_gains_the_same_as_before()
    {
        // Characterization (#145 AC12): seed buying is unchanged; passes before #145 too.
        var game = TestGames.WithGold(415);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber", 4));

        Assert.Equal(new Gold(295), game.Player.Gold);
        Assert.Equal(9, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
        Assert.Empty(game.PurchasedListings);
    }

    [Fact]
    public void BR_L5_VendorStage_of_a_null_game_throws()
    {
        Assert.Throws<ArgumentNullException>(() => ShopService.VendorStage(null!, BuildingId.GeneralStore));
    }

    [Fact]
    public void BR_L4_A_tool_listing_with_no_tool_is_never_offered_and_buying_it_is_NotOffered()
    {
        var data = TestGames.Shipped with
        {
            ShopListings =
            [
                .. TestGames.Shipped.ShopListings,
                new ShopListing("tool_nothing", ShopItemKind.Tool, null, new Gold(10), BuildingId.GeneralStore, 1, null, true),
            ],
        };
        var game = TestGames.WithGold(1000);
        var before = ToolState(game);

        Assert.DoesNotContain("tool_nothing", ShopService.Offers(game, data).Select(listing => listing.Id));
        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), ShopService.Buy(game, data, "tool_nothing"));
        Assert.Equal(before, ToolState(game));
    }

    [Fact]
    public void BR_L5_A_once_per_game_seed_listing_is_recorded_and_then_no_longer_offered()
    {
        // The once-per-game rule is the listing's, not the kind's: a seed listing flagged once per game sells once.
        var data = TestGames.Shipped with
        {
            ShopListings =
            [
                new ShopListing("seed_once", ShopItemKind.Seed, CropTypeId.Emberroot, new Gold(15), BuildingId.GeneralStore, 1, null, true),
            ],
        };
        var game = TestGames.WithGold(100);

        Assert.Equal(ActionResult.Ok, ShopService.Buy(game, data, "seed_once"));

        Assert.Equal(["seed_once"], game.PurchasedListings);
        Assert.Empty(ShopService.Offers(game, data));
    }

    [Fact]
    public void BR_L4_A_rejected_tool_purchase_through_the_session_raises_no_change_events()
    {
        var session = new GameSession(TestGames.WithBackpack(TestGames.FullBackpack(), gold: 1000), TestGames.Shipped);
        var events = RecordEvents(session);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), session.Buy(Axe));

        Assert.Empty(events);
    }
}
