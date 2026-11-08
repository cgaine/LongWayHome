using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// Selling Crops to Maggie at the general store (#146): BR-L3, BR-K2, BR-K6, BR-P2, BR-P5, the Game's SellCrop,
/// SellAllCrops and payout helpers, and GameSession's SellToMaggie, SellAllToMaggie, SellableCrops and CanSellToMaggie.
/// </summary>
public class SellToMaggieTests
{
    private static readonly IReadOnlyDictionary<CropTypeId, CropType> Crops = TestGames.CanonicalCrops;

    private static readonly GridPosition Feet = new(4, 4);

    private static ItemRef CropOf(CropTypeId id) => ItemRef.Crop(id);

    private static ItemRef SeedOf(CropTypeId id) => ItemRef.Seed(id);

    /// <summary>The canonical Crop rows without one type, for a carried Crop with no data row.</summary>
    private static IReadOnlyDictionary<CropTypeId, CropType> CropsWithout(CropTypeId id) =>
        Crops.Where(pair => pair.Key != id).ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>Gold, energy, Tiles, every Backpack Slot and the Shipping Bin, to prove a sale changed nothing.</summary>
    private static (int Gold, (int Energy, string Tiles, string Inventory) State, string Bin) Snapshot(GameState game) =>
        (game.Player.Gold.Value,
            TestGames.FullSnapshot(game),
            string.Join(",", game.ShippingBin.Items.Select(item => $"{item.CropTypeId}:{item.Quantity}")));

    private static GameSession SessionInTown(Backpack backpack, int gold = 100, GameData? data = null)
    {
        var session = new GameSession(TestGames.WithBackpack(backpack, gold: gold), data ?? TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        return session;
    }

    private static GameSession SessionOnFarm(Backpack backpack, int gold = 100) =>
        new(TestGames.WithBackpack(backpack, gold: gold), TestGames.Shipped);

    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.PlayerChanged += () => events.Add("PlayerChanged");
        session.TileChanged += position => events.Add($"TileChanged:{position}");
        session.DroppedItemsChanged += () => events.Add("DroppedItemsChanged");
        return events;
    }

    // ---- AC1 / BR-L3: selling one Crop type in town pays at once ----

    [Fact]
    public void AC1_BR_L3_selling_emberroot_in_town_pays_at_once_and_empties_the_slot()
    {
        var session = SessionInTown(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12)), gold: 100);
        var slot = session.Game.Player.Backpack.Slots.Single(s => s.Item == CropOf(CropTypeId.Emberroot)).Index;

        var result = session.SellToMaggie(CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(520, session.Game.Player.Gold.Value);
        Assert.Null(session.Game.Player.Backpack.Slots[slot].Item);
        Assert.Equal(0, session.Game.Player.Backpack.Slots[slot].Quantity);
    }

    [Fact]
    public void BR_L3_maggie_pays_the_crop_types_sell_price_per_crop()
    {
        // Dusktuber sells for 75 (canonical-values.md): 3 × 75 = 225.
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Dusktuber), 3)), gold: 0);

        var result = game.SellCrop(CropTypeId.Dusktuber, Crops);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(225, game.Player.Gold.Value);
        Assert.Equal(0, game.Player.Backpack.Quantity(CropOf(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void BR_L3_seeds_cannot_be_sold_so_only_seeds_carried_is_nothing_to_sell()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((SeedOf(CropTypeId.Emberroot), 10)), gold: 100);
        var before = Snapshot(game);

        var result = game.SellCrop(CropTypeId.Emberroot, Crops);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC1_selling_a_type_not_carried_is_nothing_to_sell_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 2)), gold: 100);
        var before = Snapshot(game);

        var result = game.SellCrop(CropTypeId.Moonmelon, Crops);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), result);
        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC2 / BR-K2: every Slot of the type is sold and emptied ----

    [Fact]
    public void AC2_BR_K2_sells_every_slot_of_the_type_and_empties_each()
    {
        var backpack = TestGames.RestoredBackpack(
            new BackpackSlot(2, CropOf(CropTypeId.Dusktuber), 99, null),
            new BackpackSlot(5, CropOf(CropTypeId.Dusktuber), 3, null));
        var game = TestGames.WithBackpack(backpack, gold: 0);

        var result = game.SellCrop(CropTypeId.Dusktuber, Crops);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(7650, game.Player.Gold.Value);
        Assert.Equal("2:-:-:0:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Equal("5:-:-:0:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[5]));
    }

    [Fact]
    public void BR_K2_sell_all_empties_every_slot_it_sells_from()
    {
        var backpack = TestGames.RestoredBackpack(
            new BackpackSlot(3, CropOf(CropTypeId.Emberroot), 99, null),
            new BackpackSlot(7, CropOf(CropTypeId.Emberroot), 1, null),
            new BackpackSlot(9, CropOf(CropTypeId.Sunbell), 4, null));
        var game = TestGames.WithBackpack(backpack, gold: 0);

        var result = game.SellAllCrops(Crops);

        Assert.Equal(ActionResult.Ok, result);
        foreach (var index in new[] { 3, 7, 9 })
        {
            Assert.Equal($"{index}:-:-:0:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[index]));
        }
    }

    // ---- AC3: sell all ----

    [Fact]
    public void AC3_sell_all_adds_the_total_and_leaves_no_crops()
    {
        var session = SessionInTown(
            TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 2), (CropOf(CropTypeId.Starcorn), 1)), gold: 100);

        var result = session.SellAllToMaggie();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(300, session.Game.Player.Gold.Value);
        Assert.Empty(session.Game.Player.Backpack.Totals(ItemKind.Crop));
    }

    [Fact]
    public void AC3_sell_all_with_no_crops_is_nothing_to_sell_and_changes_nothing()
    {
        var session = SessionInTown(TestGames.StartingBackpack(), gold: 100);
        var before = Snapshot(session.Game);

        var result = session.SellAllToMaggie();

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), result);
        Assert.Equal(before, Snapshot(session.Game));
    }

    // ---- AC4 / BR-K6: seeds and tools stay ----

    [Fact]
    public void AC4_BR_L3_selling_a_crop_type_leaves_its_seeds_in_the_backpack()
    {
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith((SeedOf(CropTypeId.Emberroot), 10), (CropOf(CropTypeId.Emberroot), 6)), gold: 0);

        var result = game.SellCrop(CropTypeId.Emberroot, Crops);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(10, game.Player.Backpack.Quantity(SeedOf(CropTypeId.Emberroot)));
        Assert.Equal(0, game.Player.Backpack.Quantity(CropOf(CropTypeId.Emberroot)));
    }

    [Fact]
    public void AC4_sell_all_leaves_every_seed_in_the_backpack()
    {
        var game = TestGames.WithBackpack(TestGames.DistinctBackpack(), gold: 0);
        var seedsBefore = game.Player.Backpack.Slots.Where(s => s.Item?.Kind == ItemKind.Seed).Select(TestGames.DescribeSlot).ToList();

        var result = game.SellAllCrops(Crops);

        Assert.Equal(ActionResult.Ok, result);
        var seedsAfter = game.Player.Backpack.Slots.Where(s => s.Item?.Kind == ItemKind.Seed).Select(TestGames.DescribeSlot).ToList();
        Assert.Equal(seedsBefore, seedsAfter);
    }

    [Fact]
    public void BR_K6_selling_never_touches_a_tool()
    {
        var backpack = TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 5));
        backpack.AddTool(ToolId.Axe, ToolTier.Basic);
        var game = TestGames.WithBackpack(backpack, gold: 0);
        var toolsBefore = game.Player.Backpack.Slots.Where(s => s.Item?.Kind == ItemKind.Tool).Select(TestGames.DescribeSlot).ToList();

        Assert.Equal(ActionResult.Ok, game.SellCrop(CropTypeId.Emberroot, Crops));
        game.Player.Backpack.Add(CropOf(CropTypeId.Sunbell), 2);
        Assert.Equal(ActionResult.Ok, game.SellAllCrops(Crops));

        var toolsAfter = game.Player.Backpack.Slots.Where(s => s.Item?.Kind == ItemKind.Tool).Select(TestGames.DescribeSlot).ToList();
        Assert.Equal(toolsBefore, toolsAfter);
    }

    // ---- AC5: a Crop type with no data row ----

    [Fact]
    public void AC5_a_crop_type_with_no_data_row_is_not_sellable_and_stays()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Moonmelon), 4)), gold: 100);
        var before = Snapshot(game);

        var result = game.SellCrop(CropTypeId.Moonmelon, CropsWithout(CropTypeId.Moonmelon));

        Assert.Equal(ActionResult.Rejected(Rejection.NotSellable), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC5_sell_all_skips_a_crop_type_with_no_data_row()
    {
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith((CropOf(CropTypeId.Moonmelon), 4), (CropOf(CropTypeId.Emberroot), 2)), gold: 0);

        var result = game.SellAllCrops(CropsWithout(CropTypeId.Moonmelon));

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(70, game.Player.Gold.Value);
        Assert.Equal(4, game.Player.Backpack.Quantity(CropOf(CropTypeId.Moonmelon)));
        Assert.Equal(0, game.Player.Backpack.Quantity(CropOf(CropTypeId.Emberroot)));
    }

    [Fact]
    public void AC5_sell_all_with_only_crops_without_a_data_row_is_nothing_to_sell()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Moonmelon), 4)), gold: 100);
        var before = Snapshot(game);

        var result = game.SellAllCrops(CropsWithout(CropTypeId.Moonmelon));

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), result);
        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC6 / BR-P5: overflow throws before anything changes ----

    [Fact]
    public void AC6_BR_P5_a_sale_that_would_overflow_gold_throws_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 1)), gold: int.MaxValue - 10);
        var before = Snapshot(game);

        Assert.Throws<OverflowException>(() => game.SellCrop(CropTypeId.Emberroot, Crops));

        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_P5_a_sell_all_that_would_overflow_gold_throws_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 1), (CropOf(CropTypeId.Starcorn), 1)),
            gold: int.MaxValue - 100);
        var before = Snapshot(game);

        Assert.Throws<OverflowException>(() => game.SellAllCrops(Crops));

        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC6_payout_helpers_throw_when_gold_plus_the_total_would_overflow()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 1)), gold: int.MaxValue - 10);
        var before = Snapshot(game);

        Assert.Throws<OverflowException>(() => game.SellPayout(CropTypeId.Emberroot, Crops));
        Assert.Throws<OverflowException>(() => game.SellAllPayout(Crops));

        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC6_a_sale_that_fits_exactly_at_the_gold_limit_succeeds()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 1)), gold: int.MaxValue - 35);

        var result = game.SellCrop(CropTypeId.Emberroot, Crops);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(int.MaxValue, game.Player.Gold.Value);
    }

    // ---- AC7 / BR-P2: no energy, no Shipping Bin ----

    [Fact]
    public void AC7_BR_P2_selling_costs_no_energy()
    {
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 3), (CropOf(CropTypeId.Sunbell), 2)), gold: 0, energy: 40);

        Assert.Equal(ActionResult.Ok, game.SellCrop(CropTypeId.Emberroot, Crops));
        Assert.Equal(40, game.Player.Energy);
        Assert.Equal(ActionResult.Ok, game.SellAllCrops(Crops));
        Assert.Equal(40, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_selling_with_no_energy_left_still_sells()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 3)), gold: 0, energy: 0);

        var result = game.SellCrop(CropTypeId.Emberroot, Crops);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(105, game.Player.Gold.Value);
        Assert.Equal(0, game.Player.Energy);
    }

    [Fact]
    public void AC7_selling_leaves_the_shipping_bin_unchanged()
    {
        var bin = new ShippingBin();
        bin.Add(CropTypeId.Dusktuber, 4);
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 3), (CropOf(CropTypeId.Dusktuber), 2)), gold: 0, bin: bin);

        Assert.Equal(ActionResult.Ok, game.SellCrop(CropTypeId.Emberroot, Crops));
        Assert.Equal(ActionResult.Ok, game.SellAllCrops(Crops));

        var item = Assert.Single(game.ShippingBin.Items);
        Assert.Equal(CropTypeId.Dusktuber, item.CropTypeId);
        Assert.Equal(4, item.Quantity);
    }

    // ---- AC8: only in town ----

    [Fact]
    public void AC8_on_the_farm_selling_one_type_is_not_offered_and_changes_nothing()
    {
        var session = SessionOnFarm(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12)));
        var events = RecordEvents(session);
        var before = Snapshot(session.Game);

        var result = session.SellToMaggie(CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, Snapshot(session.Game));
        Assert.Empty(events);
    }

    [Fact]
    public void AC8_on_the_farm_selling_all_is_not_offered_and_changes_nothing()
    {
        var session = SessionOnFarm(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12)));
        var events = RecordEvents(session);
        var before = Snapshot(session.Game);

        var result = session.SellAllToMaggie();

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, Snapshot(session.Game));
        Assert.Empty(events);
    }

    [Fact]
    public void AC8_after_leaving_town_selling_is_not_offered()
    {
        var session = SessionInTown(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12)));
        session.EnterArea(AreaId.Farm);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), session.SellToMaggie(CropTypeId.Emberroot));
        Assert.Equal(12, session.Game.Player.Backpack.Quantity(CropOf(CropTypeId.Emberroot)));
    }

    [Theory]
    [InlineData(AreaId.Farm, false)]
    [InlineData(AreaId.ForestPath, false)]
    [InlineData(AreaId.Lakeshore, false)]
    [InlineData(AreaId.Town, true)]
    public void CanSellToMaggie_is_true_only_in_town(AreaId area, bool expected)
    {
        var session = SessionOnFarm(TestGames.ToolsOnly());
        Assert.Equal(ActionResult.Ok, session.EnterArea(area));

        Assert.Equal(expected, session.CanSellToMaggie);
    }

    // ---- Events: Ok raises PlayerChanged and InventoryChanged; a rejection raises nothing ----

    [Fact]
    public void Sell_to_maggie_ok_raises_player_and_inventory_changed_events()
    {
        var session = SessionInTown(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12)));
        var events = RecordEvents(session);

        session.SellToMaggie(CropTypeId.Emberroot);

        Assert.Equal(["InventoryChanged", "PlayerChanged"], events.Order());
    }

    [Fact]
    public void Sell_all_to_maggie_ok_raises_player_and_inventory_changed_events()
    {
        var session = SessionInTown(TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12)));
        var events = RecordEvents(session);

        session.SellAllToMaggie();

        Assert.Equal(["InventoryChanged", "PlayerChanged"], events.Order());
    }

    [Fact]
    public void Sell_rejection_raises_no_events()
    {
        var session = SessionInTown(TestGames.StartingBackpack());
        var events = RecordEvents(session);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), session.SellToMaggie(CropTypeId.Emberroot));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), session.SellAllToMaggie());

        Assert.Empty(events);
    }

    [Fact]
    public void Sell_not_sellable_rejection_raises_no_events()
    {
        var session = SessionInTown(
            TestGames.BackpackWith((CropOf(CropTypeId.Moonmelon), 2)),
            data: TestGames.ShippedWithout(CropTypeId.Moonmelon));
        var events = RecordEvents(session);

        Assert.Equal(ActionResult.Rejected(Rejection.NotSellable), session.SellToMaggie(CropTypeId.Moonmelon));

        Assert.Empty(events);
        Assert.Equal(2, session.Game.Player.Backpack.Quantity(CropOf(CropTypeId.Moonmelon)));
    }

    // ---- SellableCrops ----

    [Fact]
    public void SellableCrops_lists_carried_crop_types_by_crop_type_id_with_quantity_and_payout()
    {
        var backpack = TestGames.RestoredBackpack(
            new BackpackSlot(2, CropOf(CropTypeId.Starcorn), 2, null),
            new BackpackSlot(3, CropOf(CropTypeId.Emberroot), 99, null),
            new BackpackSlot(4, SeedOf(CropTypeId.Sunbell), 5, null),
            new BackpackSlot(5, CropOf(CropTypeId.Emberroot), 1, null),
            new BackpackSlot(6, CropOf(CropTypeId.Dusktuber), 3, null));
        var session = SessionInTown(backpack);

        var rows = session.SellableCrops();

        Assert.Equal(
            [
                new SellableCrop(CropTypeId.Emberroot, "Emberroot", 100, 3500),
                new SellableCrop(CropTypeId.Dusktuber, "Dusktuber", 3, 225),
                new SellableCrop(CropTypeId.Starcorn, "Starcorn", 2, 260),
            ],
            rows);
    }

    [Fact]
    public void SellableCrops_leaves_out_crop_types_with_no_data_row()
    {
        var session = SessionInTown(
            TestGames.BackpackWith((CropOf(CropTypeId.Moonmelon), 4), (CropOf(CropTypeId.Sunbell), 2)),
            data: TestGames.ShippedWithout(CropTypeId.Moonmelon));

        var rows = session.SellableCrops();

        Assert.Equal([new SellableCrop(CropTypeId.Sunbell, "Sunbell", 2, 100)], rows);
    }

    [Fact]
    public void SellableCrops_is_empty_with_no_crops_carried()
    {
        var session = SessionInTown(TestGames.StartingBackpack());

        Assert.Empty(session.SellableCrops());
    }

    // ---- Payout helpers change nothing ----

    [Fact]
    public void Payout_of_one_type_is_quantity_times_sell_price_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith((CropOf(CropTypeId.Emberroot), 12), (CropOf(CropTypeId.Sunbell), 3)), gold: 100);
        var before = Snapshot(game);

        Assert.Equal(420, game.SellPayout(CropTypeId.Emberroot, Crops));
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void Payout_of_sell_all_is_the_total_over_types_with_a_data_row_and_changes_nothing()
    {
        var game = TestGames.WithBackpack(
            TestGames.BackpackWith(
                (CropOf(CropTypeId.Emberroot), 2), (CropOf(CropTypeId.Starcorn), 1), (CropOf(CropTypeId.Moonmelon), 5)),
            gold: 100);
        var before = Snapshot(game);

        Assert.Equal(200, game.SellAllPayout(CropsWithout(CropTypeId.Moonmelon)));
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void Payout_is_zero_when_none_are_carried_or_the_type_has_no_data_row()
    {
        var game = TestGames.WithBackpack(TestGames.BackpackWith((CropOf(CropTypeId.Moonmelon), 5)), gold: 100);

        Assert.Equal(0, game.SellPayout(CropTypeId.Emberroot, Crops));
        Assert.Equal(0, game.SellPayout(CropTypeId.Moonmelon, CropsWithout(CropTypeId.Moonmelon)));
        Assert.Equal(0, game.SellAllPayout(CropsWithout(CropTypeId.Moonmelon)));
    }

    // ---- Edge cases ----

    [Fact]
    public void Crops_dropped_on_the_ground_are_not_sold()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack(), gold: 0);
        game.Gain(CropOf(CropTypeId.Dusktuber), 5, Feet);
        Assert.Single(game.DroppedItems);

        var result = game.SellCrop(CropTypeId.Dusktuber, Crops);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), result);
        Assert.Equal(0, game.Player.Gold.Value);
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal(5, dropped.Quantity);
    }

    [Fact]
    public void Sell_all_leaves_crops_dropped_on_the_ground()
    {
        var game = TestGames.WithBackpack(TestGames.FullBackpack((CropOf(CropTypeId.Emberroot), 1)), gold: 0);
        game.Gain(CropOf(CropTypeId.Dusktuber), 5, Feet);
        var carriedPayout = game.Player.Backpack.Totals(ItemKind.Crop)
            .Sum(total => total.Quantity * Crops[total.CropTypeId].SellPrice.Value);

        Assert.Equal(ActionResult.Ok, game.SellAllCrops(Crops));

        Assert.Equal(carriedPayout, game.Player.Gold.Value);
        var dropped = Assert.Single(game.DroppedItems);
        Assert.Equal(CropOf(CropTypeId.Dusktuber), dropped.Item);
        Assert.Equal(5, dropped.Quantity);
    }
}
