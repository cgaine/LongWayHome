using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #163: <c>Game.Deliver</c> and <c>Game.DeliverGold</c> at a Rebuild sign (BR-B2, BR-B4, BR-K6, BR-P5), and
/// <c>Building.NextStage</c> / <c>RebuildDue</c>.
/// </summary>
public class DeliverTests
{
    private static readonly IReadOnlyList<BuildingStage> Stages = StoreStages;

    // ---- AC4 / BR-B2: items clamp to what is held and to what is needed ----

    [Fact]
    public void AC4_With_30_wood_held_delivering_at_stage_1_takes_the_30_and_records_30_delivered()
    {
        var game = Store(held: (Material(MaterialId.Wood), 30));

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 30, Stages);

        Assert.Equal(DeliveryResult.Ok(30), result);
        Assert.Equal(0, Held(game, MaterialId.Wood));
        Assert.Equal("GeneralStore:1:Wood=30:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void AC4_A_second_delivery_with_25_more_wood_held_takes_20_and_leaves_5()
    {
        var game = Store(held: (Material(MaterialId.Wood), 30));
        Assert.Equal(DeliveryResult.Ok(30), game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 30, Stages));
        game.Player.Backpack.Add(Material(MaterialId.Wood), 25);

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 25, Stages);

        Assert.Equal(DeliveryResult.Ok(20), result);
        Assert.Equal(5, Held(game, MaterialId.Wood));
        Assert.Equal("GeneralStore:1:Wood=50:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B2_A_delivery_is_clamped_to_what_the_backpack_holds()
    {
        var game = Store(held: (Material(MaterialId.Stone), 10));

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Stone, 25, Stages);

        Assert.Equal(DeliveryResult.Ok(10), result);
        Assert.Equal(0, Held(game, MaterialId.Stone));
        Assert.Equal("GeneralStore:1:Stone=10:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B2_A_delivery_is_clamped_to_what_is_still_needed_and_keeps_the_rest()
    {
        var game = Store(delivered: [(MaterialId.Stone, 20)], held: (Material(MaterialId.Stone), 40));

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Stone, 40, Stages);

        Assert.Equal(DeliveryResult.Ok(5), result);
        Assert.Equal(35, Held(game, MaterialId.Stone));
        Assert.Equal("GeneralStore:1:Stone=25:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B2_Delivering_gold_takes_it_from_the_player_at_once()
    {
        var game = Store(gold: 500);

        var result = game.DeliverGold(BuildingId.GeneralStore, 100, Stages);

        Assert.Equal(DeliveryResult.Ok(100), result);
        Assert.Equal(new Gold(400), game.Player.Gold);
        Assert.Equal("GeneralStore:1:-:100", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B2_Gold_is_clamped_to_what_is_still_needed()
    {
        var game = Store(gold: 500, storeGold: 250);

        var result = game.DeliverGold(BuildingId.GeneralStore, 100, Stages);

        Assert.Equal(DeliveryResult.Ok(50), result);
        Assert.Equal(new Gold(450), game.Player.Gold);
        Assert.Equal("GeneralStore:1:-:300", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B2_A_delivery_costs_no_energy_and_no_time()
    {
        var game = Store(gold: 500, held: (Material(MaterialId.Wood), 10));
        var (energy, clock) = (game.Player.Energy, game.ClockMinutes);

        game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 10, Stages);
        game.DeliverGold(BuildingId.GeneralStore, 100, Stages);

        Assert.Equal((energy, clock), (game.Player.Energy, game.ClockMinutes));
    }

    [Fact]
    public void BR_B2_A_quantity_below_1_throws_and_changes_nothing()
    {
        var game = Store(gold: 500, held: (Material(MaterialId.Wood), 10));
        var before = Snapshot(game);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 0, Stages));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.DeliverGold(BuildingId.GeneralStore, -5, Stages));
        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC6 / BR-B4: no next stage, or not in its cost ----

    [Fact]
    public void AC6_Delivering_sap_which_the_next_stage_does_not_need_is_NotOffered_and_changes_nothing()
    {
        var game = Store(held: (Material(MaterialId.Sap), 10));
        var before = Snapshot(game);

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Sap, 10, Stages);

        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), result);
        Assert.Equal(before, Snapshot(game));
        Assert.Equal(10, Held(game, MaterialId.Sap));
    }

    [Fact]
    public void AC6_Delivering_wood_to_the_carpenter_with_no_stage_row_is_NotOffered_and_changes_nothing()
    {
        var game = Store(gold: 500, held: (Material(MaterialId.Wood), 10));
        var before = Snapshot(game);

        var wood = game.Deliver(BuildingId.Carpenter, MaterialId.Wood, 10, Stages);
        var gold = game.DeliverGold(BuildingId.Carpenter, 100, Stages);

        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), wood);
        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), gold);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_B4_A_building_with_no_stage_row_has_no_next_stage()
    {
        var game = Store();

        Assert.Null(game.Buildings.Single(b => b.Id == BuildingId.Carpenter).NextStage(Stages));
        Assert.Null(game.Buildings.Single(b => b.Id == BuildingId.MirasHouse).NextStage(Stages));
        Assert.Null(game.Buildings.Single(b => b.Id == BuildingId.Blacksmith).NextStage(Stages));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    public void BR_B4_The_stores_next_stage_is_the_row_for_its_stage_plus_1(int stage, int next)
    {
        var game = Store(storeStage: stage);

        Assert.Same(Stages.Single(row => row.Stage == next), StoreOf(game).NextStage(Stages));
    }

    [Fact]
    public void BR_B4_The_store_at_its_last_stage_has_no_next_stage_and_refuses_deliveries()
    {
        var game = Store(gold: 500, storeStage: 4, held: (Material(MaterialId.Plank), 10));
        var before = Snapshot(game);

        Assert.Null(StoreOf(game).NextStage(Stages));
        Assert.Equal(
            DeliveryResult.Rejected(Rejection.NotOffered),
            game.Deliver(BuildingId.GeneralStore, MaterialId.Plank, 10, Stages));
        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), game.DeliverGold(BuildingId.GeneralStore, 100, Stages));
        Assert.Equal(before, Snapshot(game));
    }

    // ---- AC7: a complete line ----

    [Fact]
    public void AC7_Delivering_to_a_complete_item_line_is_AlreadyDelivered_and_changes_nothing()
    {
        var game = Store(delivered: [(MaterialId.Wood, 50)], held: (Material(MaterialId.Wood), 10));
        var before = Snapshot(game);

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 10, Stages);

        Assert.Equal(DeliveryResult.Rejected(Rejection.AlreadyDelivered), result);
        Assert.Equal(before, Snapshot(game));
        Assert.Equal(10, Held(game, MaterialId.Wood));
    }

    [Fact]
    public void AC7_Delivering_gold_to_a_complete_gold_line_is_AlreadyDelivered_and_changes_nothing()
    {
        var game = Store(gold: 500, storeGold: 300);
        var before = Snapshot(game);

        var result = game.DeliverGold(BuildingId.GeneralStore, 100, Stages);

        Assert.Equal(DeliveryResult.Rejected(Rejection.AlreadyDelivered), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void AC7_A_later_delivery_never_lowers_what_was_delivered_before()
    {
        var game = Store(gold: 500, held: [(Material(MaterialId.Wood), 30), (Material(MaterialId.Stone), 10)]);
        game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 30, Stages);

        game.Deliver(BuildingId.GeneralStore, MaterialId.Stone, 10, Stages);
        game.DeliverGold(BuildingId.GeneralStore, 100, Stages);
        game.Deliver(BuildingId.GeneralStore, MaterialId.Sap, 1, Stages);

        Assert.Equal("GeneralStore:1:Wood=30;Stone=10:100", Describe(StoreOf(game)));
    }

    // ---- 0 held ----

    [Fact]
    public void Delivering_a_needed_material_with_none_held_is_NothingToDeliver_and_changes_nothing()
    {
        var game = Store(gold: 500);
        var before = Snapshot(game);

        var result = game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 10, Stages);

        Assert.Equal(DeliveryResult.Rejected(Rejection.NothingToDeliver), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void Delivering_gold_with_none_held_is_NothingToDeliver_and_changes_nothing()
    {
        var game = Store(gold: 0);
        var before = Snapshot(game);

        var result = game.DeliverGold(BuildingId.GeneralStore, 100, Stages);

        Assert.Equal(DeliveryResult.Rejected(Rejection.NothingToDeliver), result);
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void BR_P5_Gold_delivered_is_clamped_to_the_gold_held_and_never_goes_below_0()
    {
        var game = Store(gold: 30);

        var result = game.DeliverGold(BuildingId.GeneralStore, 100, Stages);

        Assert.Equal(DeliveryResult.Ok(30), result);
        Assert.Equal(new Gold(0), game.Player.Gold);
        Assert.Equal("GeneralStore:1:-:30", Describe(StoreOf(game)));
    }

    // ---- BR-K6: tools ----

    [Fact]
    public void BR_K6_Delivering_every_line_never_touches_the_tools_in_the_backpack()
    {
        var game = Store(gold: 500, held: [(Material(MaterialId.Wood), 60), (Material(MaterialId.Stone), 30)]);
        var tools = game.Player.Backpack.Slots.Where(slot => slot.Item?.Kind == ItemKind.Tool).Select(TestGames.DescribeSlot).ToList();
        Assert.Equal(2, tools.Count);

        game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 60, Stages);
        game.Deliver(BuildingId.GeneralStore, MaterialId.Stone, 30, Stages);
        game.DeliverGold(BuildingId.GeneralStore, 300, Stages);

        Assert.Equal(
            tools,
            game.Player.Backpack.Slots.Where(slot => slot.Item?.Kind == ItemKind.Tool).Select(TestGames.DescribeSlot));
        Assert.Equal("GeneralStore:1:Wood=50;Stone=25:300", Describe(StoreOf(game)));
    }

    // ---- RebuildDue (BR-B3 reads it) ----

    [Fact]
    public void RebuildDue_is_false_until_the_full_next_stage_cost_is_delivered()
    {
        var partial = Store(storeGold: 300, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 24)]);
        var full = Store(storeGold: 300, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 25)]);

        Assert.False(StoreOf(partial).RebuildDue(Stages));
        Assert.True(StoreOf(full).RebuildDue(Stages));
    }

    [Fact]
    public void RebuildDue_is_false_for_a_building_with_no_next_stage()
    {
        var game = Store(storeStage: 4);

        Assert.False(StoreOf(game).RebuildDue(Stages));
        Assert.False(game.Buildings.Single(b => b.Id == BuildingId.Carpenter).RebuildDue(Stages));
    }
}
