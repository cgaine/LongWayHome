using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
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
        Assert.All(offers.SkipLast(1), listing => Assert.Equal(ShopItemKind.Seed, listing.Kind));
    }

    [Fact]
    public void AC3_BR_L2_buying_emberroot_in_a_new_game_takes_15_gold_and_adds_one_seed()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(85), game.Player.Gold);
        Assert.Single(TestGames.Stacks(game.Player.Backpack), stack => stack.Kind == ItemKind.Seed && stack.CropTypeId == CropTypeId.Emberroot);
        Assert.Equal(11, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
    }

    /// <summary>Tools, 90 emberroot seeds in Slot 2, every other Slot full: room for exactly 9 more emberroot seeds.</summary>
    private static Backpack RoomForNineEmberrootSeeds() =>
        TestGames.FullBackpack((ItemRef.Seed(CropTypeId.Emberroot), 90));

    [Fact]
    public void AC9_Buying_seeds_that_do_not_all_fit_is_BackpackFull_and_takes_no_gold()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000);
        var before = TestGames.Describe(game.Player.Backpack);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 10);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(new Gold(1000), game.Player.Gold);
        Assert.Equal(before, TestGames.Describe(game.Player.Backpack));
    }

    [Fact]
    public void AC9_Buying_a_seed_type_with_no_free_slot_is_BackpackFull_and_takes_no_gold()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber");

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(new Gold(1000), game.Player.Gold);
        Assert.Equal(0, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void AC9_Session_buy_of_seeds_that_do_not_fit_raises_nothing()
    {
        var session = new GameSession(TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 1000), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.Buy("seed_emberroot", 10);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Empty(events);
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
    public void BR_P5_Exactly_enough_gold_but_no_room_is_BackpackFull_and_keeps_the_gold()
    {
        var game = TestGames.WithBackpack(RoomForNineEmberrootSeeds(), gold: 10 * 15);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 10);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(new Gold(150), game.Player.Gold);
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
        Assert.DoesNotContain(HeldItem.SeedOf(CropTypeId.Sunbell), session.HeldItems());

        var result = session.Buy("seed_sunbell");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Sunbell)));
        Assert.Contains(HeldItem.SeedOf(CropTypeId.Sunbell), session.HeldItems());
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
    public void BR_L2_a_seed_stack_past_what_the_backpack_holds_is_refused_and_takes_no_gold()
    {
        // BR-K2: a Slot holds at most 99, so every Slot full of emberroot seeds is as many as can be carried.
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Emberroot), 10 * Backpack.MaxStack);
        var game = TestGames.WithGold(100, TestGames.FirstDay, inventory);
        var before = Snapshot(game);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(before, Snapshot(game));
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

        Assert.Equal(["seed_emberroot", "seed_dusktuber", CanUpgrade], OfferIds(game));
    }

    [Fact]
    public void AC1_winter_offers_the_can_upgrade_alone()
    {
        var game = TestGames.WithGold(1000, SeasonDay(Season.Winter));

        Assert.Equal([CanUpgrade], OfferIds(game));
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

        Assert.Equal([.. seeds, CanUpgrade], OfferIds(game));
    }

    [Fact]
    public void BR_L1_the_can_upgrade_is_not_offered_once_upgraded()
    {
        var game = TestGames.WithGold(1000, TestGames.FirstDay, can: UpgradedCan(40));

        Assert.Equal(["seed_emberroot", "seed_dusktuber"], OfferIds(game));
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

        var result = session.RefillCan();

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

        Assert.Equal(["seed_emberroot", "seed_dusktuber", CanUpgrade], offers);
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
}
