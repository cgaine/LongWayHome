using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// Dropped items through <see cref="GameSession"/> and <see cref="ShopService"/> (#130): Use and Buy drop at the
/// farmer's feet, <c>PickUp</c> and the <c>DroppedItemsChanged</c> event.
/// </summary>
public class DroppedItemSessionTests
{
    private static readonly GridPosition Target = new(5, 2);

    private static readonly GridPosition Feet = new(1, 7);

    private static readonly ItemRef EmberrootSeeds = ItemRef.Seed(CropTypeId.Emberroot);

    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.PlayerChanged += () => events.Add("PlayerChanged");
        session.DroppedItemsChanged += () => events.Add("DroppedItemsChanged");
        session.TileChanged += position => events.Add($"TileChanged:{position}");
        return events;
    }

    /// <summary>Tools, 69 emberroot seeds in Slot 2, every other Slot full: room for exactly 30 more emberroot seeds.</summary>
    private static Backpack RoomForThirtyEmberrootSeeds() => TestGames.FullBackpack((EmberrootSeeds, 69));

    /// <summary>A session whose full Backpack has one emberroot crop lying at <see cref="Feet"/>, after freeing Slot 11.</summary>
    private static GameSession SessionWithOneCropAtFeetAndRoom()
    {
        var session = TestGames.SessionWithBackpack(TestGames.FullBackpack());
        session.Game.Gain(ItemRef.Crop(CropTypeId.Emberroot), 1, Feet);
        Assert.True(session.Game.Player.Backpack.Remove(TestGames.Filler, 99));
        return session;
    }

    // ---- AC1: Use on a ripe tile with no room harvests and drops ----

    [Fact]
    public void AC1_use_on_ripe_tile_with_full_backpack_harvests_and_drops()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.FullBackpack(), TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Dusktuber)));
        var events = RecordEvents(session);

        var preview = session.PreviewUse(Target, 0);
        var result = session.Use(Target, 0, Feet);

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), preview);
        Assert.Equal(ActionResult.Ok, result);
        Assert.Null(session.Game.FarmPlot[Target].Planting);
        var dropped = Assert.Single(session.Game.DroppedItems);
        Assert.Equal((Feet, ItemRef.Crop(CropTypeId.Dusktuber), 1), (dropped.Position, dropped.Item, dropped.Quantity));
        Assert.Contains("DroppedItemsChanged", events);
        Assert.Contains($"TileChanged:{Target}", events);
    }

    [Fact]
    public void BR_T6_session_harvest_with_room_raises_no_dropped_items_changed()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.StartingBackpack(), TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Dusktuber)));
        var events = RecordEvents(session);

        var result = session.Harvest(Target, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Empty(session.Game.DroppedItems);
        Assert.DoesNotContain("DroppedItemsChanged", events);
        Assert.Contains("InventoryChanged", events);
    }

    // ---- AC4: buying more than fits takes the full price and drops the rest ----

    [Fact]
    public void AC4_buying_seeds_that_dont_fit_takes_full_price_and_drops_the_rest()
    {
        var session = new GameSession(
            TestGames.WithBackpack(RoomForThirtyEmberrootSeeds(), gold: 2000), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.Buy("seed_emberroot", 120, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(2000 - (120 * 15)), session.Game.Player.Gold);
        Assert.Equal(99, session.Game.Player.Backpack.Quantity(EmberrootSeeds));
        var dropped = Assert.Single(session.Game.DroppedItems);
        Assert.Equal((Feet, EmberrootSeeds, 90), (dropped.Position, dropped.Item, dropped.Quantity));
        Assert.True(session.LastBuyDropped);
        Assert.Contains("DroppedItemsChanged", events);
        Assert.Contains("PlayerChanged", events);
        Assert.Contains("InventoryChanged", events);
    }

    [Fact]
    public void AC4_shop_service_buy_drops_the_rest_at_the_given_feet()
    {
        var game = TestGames.WithBackpack(RoomForThirtyEmberrootSeeds(), gold: 2000);

        var result = ShopService.Buy(game, TestGames.Shipped, "seed_emberroot", 120, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(new Gold(200), game.Player.Gold);
        Assert.Equal(90, Assert.Single(game.DroppedItems).Quantity);
    }

    [Fact]
    public void AC4_a_purchase_that_all_fits_drops_nothing_and_raises_no_dropped_items_changed()
    {
        var session = new GameSession(TestGames.WithBackpack(TestGames.StartingBackpack(), gold: 100), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.Buy("seed_emberroot", 2, Feet);

        Assert.Equal(ActionResult.Ok, result);
        Assert.False(session.LastBuyDropped);
        Assert.Empty(session.Game.DroppedItems);
        Assert.DoesNotContain("DroppedItemsChanged", events);
    }

    // ---- AC5, AC7: picking up through the session ----

    [Fact]
    public void AC5_session_pick_up_raises_inventory_and_dropped_items_changed()
    {
        var session = SessionWithOneCropAtFeetAndRoom();
        Assert.True(session.HasDroppedItemsAt(Feet));
        var events = RecordEvents(session);

        var picked = session.PickUp(Feet);

        Assert.True(picked);
        Assert.Empty(session.Game.DroppedItems);
        Assert.False(session.HasDroppedItemsAt(Feet));
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
        Assert.Contains("InventoryChanged", events);
        Assert.Contains("DroppedItemsChanged", events);
    }

    [Fact]
    public void AC7_session_pick_up_on_an_empty_tile_raises_nothing()
    {
        var session = SessionWithOneCropAtFeetAndRoom();
        var elsewhere = new GridPosition(10, 0);
        Assert.False(session.HasDroppedItemsAt(elsewhere));
        var events = RecordEvents(session);

        var picked = session.PickUp(elsewhere);

        Assert.False(picked);
        Assert.Empty(events);
        Assert.Single(session.Game.DroppedItems);
    }

    [Fact]
    public void AC7_session_pick_up_without_room_raises_nothing_and_costs_no_energy()
    {
        var session = TestGames.SessionWithBackpack(TestGames.FullBackpack());
        session.Game.Gain(ItemRef.Crop(CropTypeId.Emberroot), 4, Feet);
        var energy = session.Game.Player.Energy;
        var events = RecordEvents(session);

        var picked = session.PickUp(Feet);

        Assert.False(picked);
        Assert.Empty(events);
        Assert.Equal(energy, session.Game.Player.Energy);
        Assert.Equal(4, Assert.Single(session.Game.DroppedItems).Quantity);
    }
}
