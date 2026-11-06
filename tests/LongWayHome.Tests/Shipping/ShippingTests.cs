using System.Reflection;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Shipping;

/// <summary>Shipping crops at the bin (#44): BR-S1, BR-S2, BR-P2, BR-P7, and GameSession.ShipAll's events.</summary>
public class ShippingTests
{
    private static readonly GridPosition Target = new(6, 3);

    /// <summary>9 emberroot crops, 2 dusktuber crops and 10 emberroot seeds.</summary>
    private static Inventory Basket()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 9);
        inventory.Add(ItemKind.Crop, CropTypeId.Dusktuber, 2);
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 10);
        return inventory;
    }

    private static Inventory SeedsOnly()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 10);
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 5);
        return inventory;
    }

    private static string Stacks(GameState game) =>
        string.Join(",", game.Player.Inventory.Stacks.Select(stack => $"{stack.Kind}:{stack.CropTypeId}:{stack.Quantity}"));

    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.PlayerChanged += () => events.Add("PlayerChanged");
        session.TileChanged += position => events.Add($"TileChanged:{position}");
        return events;
    }

    [Fact]
    public void AC1_ship_all_moves_every_crop_into_the_bin_and_leaves_the_seeds()
    {
        var game = TestGames.WithEnergy(150, Basket());

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(
            [new InventoryStack(ItemKind.Seed, CropTypeId.Emberroot, 10)],
            game.Player.Inventory.Stacks);
        Assert.Equal(
            [new ShippedItem(CropTypeId.Emberroot, 9), new ShippedItem(CropTypeId.Dusktuber, 2)],
            game.ShippingBin.Items);
    }

    [Fact]
    public void AC2_ship_all_with_only_seeds_is_nothing_to_ship_and_changes_nothing()
    {
        var game = TestGames.WithEnergy(150, SeedsOnly());
        var before = Stacks(game);

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToShip), result);
        Assert.Equal(before, Stacks(game));
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void AC2_ship_all_with_an_empty_inventory_is_nothing_to_ship_and_keeps_the_bin()
    {
        var game = TestGames.WithShippingBin(150, new Inventory(), (CropTypeId.Emberroot, 3));

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToShip), result);
        Assert.Empty(game.Player.Inventory.Stacks);
        Assert.Equal([new ShippedItem(CropTypeId.Emberroot, 3)], game.ShippingBin.Items);
    }

    [Fact]
    public void AC3_shipping_twice_in_a_day_merges_into_one_bin_item()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 3);
        var game = TestGames.WithTiles(
            150,
            inventory,
            TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)),
            TestGames.TilledWith(new GridPosition(7, 3), TestGames.Ripe(CropTypeId.Emberroot)));
        Assert.Equal(ActionResult.Ok, game.ShipAll());

        Assert.Equal(ActionResult.Ok, game.Harvest(Target, TestGames.Crop(CropTypeId.Emberroot)));
        Assert.Equal(ActionResult.Ok, game.Harvest(new GridPosition(7, 3), TestGames.Crop(CropTypeId.Emberroot)));
        var result = game.ShipAll();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal([new ShippedItem(CropTypeId.Emberroot, 5)], game.ShippingBin.Items);
    }

    [Fact]
    public void AC4_ship_all_changes_neither_energy_nor_gold()
    {
        var game = TestGames.WithEnergy(37, Basket());

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(37, game.Player.Energy);
        Assert.Equal(new Gold(100), game.Player.Gold);
    }

    [Fact]
    public void AC9_session_ship_all_raises_inventory_changed_once_on_success()
    {
        var session = new GameSession(TestGames.WithEnergy(150, Basket()), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.ShipAll();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(["InventoryChanged"], events);
        Assert.Equal(
            [new ShippedItem(CropTypeId.Emberroot, 9), new ShippedItem(CropTypeId.Dusktuber, 2)],
            session.Game.ShippingBin.Items);
    }

    [Fact]
    public void AC9_session_ship_all_with_nothing_to_ship_raises_no_event()
    {
        var session = new GameSession(TestGames.WithEnergy(150, SeedsOnly()), TestGames.Shipped);
        var events = RecordEvents(session);

        var result = session.ShipAll();

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToShip), result);
        Assert.Empty(events);
    }

    [Fact]
    public void BR_S1_shipping_leaves_seeds_in_the_inventory()
    {
        var inventory = SeedsOnly();
        inventory.Add(ItemKind.Crop, CropTypeId.Dusktuber, 4);
        var game = TestGames.WithEnergy(150, inventory);

        game.ShipAll();

        Assert.Equal(10, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
        Assert.Equal(5, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
        Assert.Equal(0, game.Player.Inventory.Quantity(ItemKind.Crop, CropTypeId.Dusktuber));
        Assert.Equal([new ShippedItem(CropTypeId.Dusktuber, 4)], game.ShippingBin.Items);
    }

    [Fact]
    public void BR_S1_the_shipping_bin_has_no_way_to_take_a_single_item_back_out()
    {
        // Structural guard: only Add, Empty and the Items getter are public, beyond what every object has.
        var objectMethods = typeof(object).GetMethods().Select(method => method.Name).ToHashSet();
        var methods = typeof(ShippingBin)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Where(name => !objectMethods.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["Add", "Empty", "get_Items"], methods);
    }

    [Fact]
    public void BR_S2_pay_out_adds_quantity_times_sell_price_and_empties_the_bin()
    {
        var game = TestGames.WithShippingBin(
            150, new Inventory(), (CropTypeId.Emberroot, 9), (CropTypeId.Dusktuber, 2));

        var paid = game.PayOutShippingBin(TestGames.CanonicalCrops);

        Assert.Equal(465, paid);
        Assert.Equal(new Gold(100 + 465), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void BR_S2_pay_out_of_an_empty_bin_pays_nothing()
    {
        var game = TestGames.WithEnergy(150);

        var paid = game.PayOutShippingBin(TestGames.CanonicalCrops);

        Assert.Equal(0, paid);
        Assert.Equal(new Gold(100), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void BR_S2_pay_out_of_an_item_with_no_data_row_pays_zero_for_it_and_removes_it()
    {
        var game = TestGames.WithShippingBin(
            150, new Inventory(), (CropTypeId.Emberroot, 2), (CropTypeId.Starcorn, 3));
        var withoutStarcorn = TestGames.CanonicalCrops
            .Where(pair => pair.Key != CropTypeId.Starcorn)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var paid = game.PayOutShippingBin(withoutStarcorn);

        Assert.Equal(70, paid);
        Assert.Equal(new Gold(170), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void BR_S2_shipping_bin_add_merges_by_crop_type_and_keeps_crop_type_order()
    {
        var bin = new ShippingBin();

        bin.Add(CropTypeId.Starcorn, 1);
        bin.Add(CropTypeId.Emberroot, 3);
        bin.Add(CropTypeId.Dusktuber, 2);
        bin.Add(CropTypeId.Emberroot, 4);

        Assert.Equal(
            [
                new ShippedItem(CropTypeId.Emberroot, 7),
                new ShippedItem(CropTypeId.Dusktuber, 2),
                new ShippedItem(CropTypeId.Starcorn, 1),
            ],
            bin.Items);
    }

    [Fact]
    public void BR_S2_shipping_bin_empty_removes_every_item()
    {
        var bin = new ShippingBin();
        bin.Add(CropTypeId.Emberroot, 3);
        bin.Add(CropTypeId.Moonmelon, 1);

        bin.Empty();

        Assert.Empty(bin.Items);
    }

    [Fact]
    public void BR_P2_shipping_costs_no_energy_and_ships_even_at_zero_energy()
    {
        var game = TestGames.WithEnergy(0, Basket());

        var result = game.ShipAll();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(0, game.Player.Energy);
        Assert.Equal(
            [new ShippedItem(CropTypeId.Emberroot, 9), new ShippedItem(CropTypeId.Dusktuber, 2)],
            game.ShippingBin.Items);
    }

    [Fact]
    public void BR_P7_after_ship_all_no_crop_stack_remains_and_every_stack_is_at_least_1()
    {
        var game = TestGames.WithEnergy(150, Basket());

        game.ShipAll();

        Assert.DoesNotContain(game.Player.Inventory.Stacks, stack => stack.Kind == ItemKind.Crop);
        Assert.All(game.Player.Inventory.Stacks, stack => Assert.True(stack.Quantity >= 1));
        Assert.NotEmpty(game.Player.Inventory.Stacks);
    }

    [Fact]
    public void Shipped_item_with_quantity_0_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShippedItem(CropTypeId.Emberroot, 0));
    }

    [Fact]
    public void Shipped_item_keeps_its_crop_type_and_quantity()
    {
        var item = new ShippedItem(CropTypeId.Dusktuber, 4);

        Assert.Equal(CropTypeId.Dusktuber, item.CropTypeId);
        Assert.Equal(4, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Shipping_bin_add_with_quantity_below_1_throws_and_changes_nothing(int quantity)
    {
        var bin = new ShippingBin();
        bin.Add(CropTypeId.Emberroot, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => bin.Add(CropTypeId.Emberroot, quantity));
        Assert.Equal([new ShippedItem(CropTypeId.Emberroot, 2)], bin.Items);
    }

    [Fact]
    public void A_game_constructed_without_a_bin_has_an_empty_bin()
    {
        var game = TestGames.WithEnergy(150);

        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void A_new_game_has_an_empty_bin()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        Assert.Empty(game.ShippingBin.Items);
    }
}
