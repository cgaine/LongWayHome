using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Shop;

/// <summary>The seeds shop (#45): BR-L1, BR-L2, BR-P2, BR-P5, BR-P7, and GameSession.Buy's events.</summary>
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

        Assert.Equal(["seed_emberroot", "seed_dusktuber"], OfferIds(game));
    }

    [Theory]
    [InlineData(Season.Summer, new[] { "seed_sunbell", "seed_starcorn" })]
    [InlineData(Season.Fall, new[] { "seed_moonmelon" })]
    [InlineData(Season.Winter, new string[0])]
    public void AC2_BR_L1_shop_offers_the_seeds_of_the_current_season_in_listing_order(Season season, string[] expected)
    {
        var game = TestGames.WithGold(100, SeasonDay(season));

        Assert.Equal(expected, OfferIds(game));
    }

    [Fact]
    public void BR_L1_a_seed_listing_whose_crop_type_has_no_data_row_is_not_offered()
    {
        var game = TestGames.WithGold(100, TestGames.FirstDay);
        var data = TestGames.ShippedWithout(CropTypeId.Emberroot);

        var offers = ShopService.Offers(game, data).Select(listing => listing.Id);

        Assert.Equal(["seed_dusktuber"], offers);
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Summer)]
    [InlineData(Season.Fall)]
    [InlineData(Season.Winter)]
    public void BR_L1_the_can_upgrade_is_never_offered_by_the_seeds_shop(Season season)
    {
        var game = TestGames.WithGold(1000, SeasonDay(season));

        var offers = ShopService.Offers(game, TestGames.Shipped);

        Assert.All(offers, listing => Assert.Equal(ShopItemKind.Seed, listing.Kind));
        Assert.DoesNotContain(offers, listing => listing.Id == "can_upgrade");
    }

    [Fact]
    public void AC3_BR_L2_buying_emberroot_in_a_new_game_takes_15_gold_and_adds_one_seed()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(85), game.Player.Gold);
        Assert.Single(game.Player.Inventory.Stacks, stack => stack.Kind == ItemKind.Seed && stack.CropTypeId == CropTypeId.Emberroot);
        Assert.Equal(11, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
    }

    [Fact]
    public void BR_P7_bought_seeds_merge_into_the_existing_stack()
    {
        var game = TestGames.WithGold(100);

        ShopService.Buy(game, TestGames.Shipped, "seed_dusktuber");

        Assert.Equal(
            [
                new InventoryStack(ItemKind.Seed, CropTypeId.Emberroot, 10),
                new InventoryStack(ItemKind.Seed, CropTypeId.Dusktuber, 6),
            ],
            game.Player.Inventory.Stacks);
    }

    [Fact]
    public void BR_P7_buying_a_seed_type_not_held_creates_a_new_stack_that_becomes_holdable()
    {
        var session = new GameSession(TestGames.WithGold(100, SeasonDay(Season.Summer)), TestGames.Shipped);
        Assert.DoesNotContain(HeldItem.SeedOf(CropTypeId.Sunbell), session.HeldItems());

        var result = session.Buy("seed_sunbell");

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(1, session.Game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Sunbell));
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
        Assert.Equal(6, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
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
    [InlineData("can_upgrade")]
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
        Assert.Equal(9, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
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
    public void BR_L2_a_seed_stack_past_int_max_throws_overflow_and_takes_no_gold()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, int.MaxValue);
        var game = TestGames.WithGold(100, TestGames.FirstDay, inventory);
        var before = Snapshot(game);

        Assert.Throws<OverflowException>(() => ShopService.Buy(game, TestGames.Shipped, "seed_emberroot"));
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
        Assert.Equal(11, session.Game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
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
        Assert.Equal(9, session.Game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
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
}
