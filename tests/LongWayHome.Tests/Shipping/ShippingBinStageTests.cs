using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Shipping;

/// <summary>
/// #166 AC6: the farm Shipping Bin exists only once the general store is at stage 4 (BR-S3); there it ships every Crop
/// (BR-S1) and the Night pays their sellPrice (BR-S2) as in Phase 1.
/// </summary>
public class ShippingBinStageTests
{
    /// <summary>The tools, 9 emberroot crops, 2 dusktuber crops and 10 emberroot seeds.</summary>
    private static Backpack Basket() => TestGames.BackpackWith(
        (ItemRef.Crop(CropTypeId.Emberroot), 9),
        (ItemRef.Crop(CropTypeId.Dusktuber), 2),
        (ItemRef.Seed(CropTypeId.Emberroot), 10));

    private static GameState AtStage(int stage) =>
        BuildingGames.WithBuildings(BuildingGames.All(storeStage: stage), Basket(), gold: 100);

    private static (int Gold, string Backpack, string Bin, int Energy) Snapshot(GameState game) =>
        (game.Player.Gold.Value,
            TestGames.Describe(game.Player.Backpack),
            string.Join(",", game.ShippingBin.Items.Select(item => $"{item.CropTypeId}:{item.Quantity}")),
            game.Player.Energy);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AC6_Before_store_stage_4_ShipAll_with_crops_held_is_NotOffered_and_changes_nothing(int stage)
    {
        var game = AtStage(stage);
        var before = Snapshot(game);

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC6_At_store_stage_4_ShipAll_ships_every_crop_and_keeps_the_seeds()
    {
        var game = AtStage(4);

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(
            [new ShippedItem(CropTypeId.Emberroot, 9), new ShippedItem(CropTypeId.Dusktuber, 2)],
            game.ShippingBin.Items);
        Assert.Equal([(ItemKind.Seed, CropTypeId.Emberroot, 10)], TestGames.Stacks(game.Player.Backpack));
    }

    [Fact]
    public void AC6_At_store_stage_4_the_Night_pays_the_shipped_crops_sell_price()
    {
        var session = new GameSession(AtStage(4), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.ShipAll());

        session.Sleep();

        // emberroot 9 × 35 + dusktuber 2 × 75 = 465.
        Assert.Equal(new Gold(100 + 465), session.Game.Player.Gold);
        Assert.Empty(session.Game.ShippingBin.Items);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void BR_S3_There_is_no_farm_shipping_bin_before_store_stage_4(int stage)
    {
        Assert.False(AtStage(stage).HasShippingBin);
    }

    [Fact]
    public void BR_S3_The_farm_shipping_bin_exists_at_store_stage_4()
    {
        Assert.True(AtStage(4).HasShippingBin);
    }

    [Fact]
    public void BR_S3_A_new_game_has_no_farm_shipping_bin()
    {
        Assert.False(TestGames.NewGameWithSeeds().HasShippingBin);
    }

    [Fact]
    public void BR_S3_Session_ShipAll_before_store_stage_4_is_NotOffered_and_raises_no_event()
    {
        var session = new GameSession(AtStage(3), TestGames.Shipped);
        var events = new List<string>();
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.PlayerChanged += () => events.Add("PlayerChanged");

        var result = session.ShipAll();

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Empty(events);
    }

    [Fact]
    public void BR_S3_ShipAll_before_store_stage_4_with_nothing_to_ship_is_NotOffered()
    {
        var game = BuildingGames.WithBuildings(BuildingGames.All(storeStage: 1), TestGames.StartingBackpack());

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), game.ShipAll());
    }

    [Fact]
    public void BR_S2_Before_store_stage_4_the_Night_pays_nothing_for_crops_held()
    {
        var session = new GameSession(AtStage(3), TestGames.Shipped);
        session.ShipAll();

        session.Sleep();

        Assert.Equal(new Gold(100), session.Game.Player.Gold);
        Assert.Equal(9, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }
}
