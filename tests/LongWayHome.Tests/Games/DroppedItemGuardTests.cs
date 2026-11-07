using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// The guards and paths of #130's dropped items the acceptance tests don't drive: invalid gains, the DroppedItem
/// invariants on area and mine level, a pick-up that skips an item with no room, a refused purchase, and
/// <see cref="GameSession.LastBuyDropped"/> across purchases.
/// </summary>
public class DroppedItemGuardTests
{
    private static readonly GridPosition Feet = new(4, 6);

    private static readonly ItemRef EmberrootCrop = ItemRef.Crop(CropTypeId.Emberroot);

    private static readonly ItemRef DusktuberCrop = ItemRef.Crop(CropTypeId.Dusktuber);

    private static GameState FullGame(params (ItemRef Item, int Quantity)[] items) =>
        TestGames.WithBackpack(TestGames.FullBackpack(items));

    // ---- Gain's guards ----

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void BR_K3_gain_of_less_than_one_throws_and_changes_nothing(int quantity)
    {
        var game = FullGame();
        var before = TestGames.Describe(game.Player.Backpack);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.Gain(EmberrootCrop, quantity, Feet));

        Assert.Equal(before, TestGames.Describe(game.Player.Backpack));
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_K3_gain_of_an_item_whose_id_does_not_match_its_kind_throws_and_drops_nothing()
    {
        var game = FullGame();

        Assert.Throws<ArgumentException>(() => game.Gain(new ItemRef(ItemKind.Seed, null, ToolId.Hoe), 3, Feet));

        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void BR_K3_gain_with_no_room_reports_nothing_added_and_every_drop()
    {
        var game = FullGame();

        var result = game.Gain(DusktuberCrop, 99, Feet);

        Assert.Equal(0, result.Added);
        Assert.Equal(99, Assert.Single(result.Dropped).Quantity);
    }

    // ---- DroppedItem's invariants ----

    [Fact]
    public void BR_K6_dropped_item_with_an_invalid_item_throws()
    {
        Assert.Throws<ArgumentException>(
            () => new DroppedItem(AreaId.Farm, null, Feet, new ItemRef(ItemKind.Crop, null, null), 1));
    }

    [Fact]
    public void REQ_47_dropped_item_in_an_unknown_area_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DroppedItem((AreaId)99, null, Feet, EmberrootCrop, 1));
    }

    [Theory]
    [InlineData(AreaId.Farm, 1)]
    [InlineData(AreaId.Town, 3)]
    [InlineData(AreaId.Mine, null)]
    [InlineData(AreaId.Mine, 0)]
    [InlineData(AreaId.Mine, 6)]
    public void REQ_47_dropped_item_mine_level_is_1_to_5_in_the_mine_and_empty_elsewhere(AreaId area, int? mineLevel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DroppedItem(area, mineLevel, Feet, EmberrootCrop, 1));
    }

    [Theory]
    [InlineData(AreaId.Mine, 1)]
    [InlineData(AreaId.Mine, 5)]
    [InlineData(AreaId.Lakeshore, null)]
    public void REQ_47_dropped_item_accepts_a_valid_area_and_mine_level(AreaId area, int? mineLevel)
    {
        var item = new DroppedItem(area, mineLevel, Feet, EmberrootCrop, 1);

        Assert.Equal((area, mineLevel), (item.Area, item.MineLevel));
    }

    // ---- PickUp goes on past an item with no room ----

    [Fact]
    public void BR_K4_pick_up_skips_an_item_with_no_room_and_still_tops_up_a_later_matching_stack()
    {
        // Slot 2 holds 90 emberroot crops; the dusktuber drops whole, the emberroot tops Slot 2 to 99 and drops 6.
        var game = FullGame((EmberrootCrop, 90));
        game.Gain(DusktuberCrop, 2, Feet);
        game.Gain(EmberrootCrop, 15, Feet);
        Assert.True(game.Player.Backpack.Remove(EmberrootCrop, 5)); // room for 5 emberroot, none for dusktuber

        Assert.True(game.PickUp(Feet));

        Assert.Equal(99, game.Player.Backpack.Quantity(EmberrootCrop));
        Assert.Equal(
            [(DusktuberCrop, 2), (EmberrootCrop, 1)],
            game.DroppedItems.Select(item => (item.Item, item.Quantity)));
    }

    // ---- Buying ----

    [Fact]
    public void BR_L2_not_enough_gold_with_a_full_backpack_is_refused_and_drops_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 10);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 1, Feet);

        Assert.Equal(ActionResult.Rejected(Rejection.NotEnoughGold), result);
        Assert.Equal(new Gold(10), game.Player.Gold);
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void AC4_last_buy_dropped_follows_the_last_successful_purchase_only()
    {
        // Room for 9 emberroot seeds: buying 10 drops 1.
        var game = TestGames.WithBackpack(TestGames.FullBackpack((ItemRef.Seed(CropTypeId.Emberroot), 90)), gold: 1000);
        var session = new GameSession(game, TestGames.Shipped);
        Assert.False(session.LastBuyDropped);

        Assert.True(session.Buy("seed_emberroot", 10, Feet).IsOk);
        Assert.True(session.LastBuyDropped);

        // A refused purchase leaves it as the last successful one set it.
        Assert.False(session.Buy("seed_emberroot", 99, Feet).IsOk);
        Assert.True(session.LastBuyDropped);

        // The Can upgrade drops nothing.
        Assert.True(session.Buy("can_upgrade", 1, Feet).IsOk);
        Assert.False(session.LastBuyDropped);
    }

    // ---- Layout and icons ----

    [Fact]
    public void AC12_a_negative_index_on_a_tile_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DroppedItemLayout.Offset(-1));
    }

    [Fact]
    public void AC12_an_item_whose_id_does_not_match_its_kind_has_no_icon()
    {
        Assert.Null(ItemIcons.For(new ItemRef(ItemKind.Crop, null, ToolId.Hoe)));
    }
}
