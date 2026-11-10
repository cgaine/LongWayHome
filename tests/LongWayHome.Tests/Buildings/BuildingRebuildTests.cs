using LongWayHome.Domain.Buildings;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Buildings;

/// <summary>
/// #165: the Night's rebuild step, <c>Game.RebuildDue</c>. A Building whose next stage is fully delivered rises one
/// stage and has its deliveries cleared (BR-B3), one stage at a time and never past its last row (BR-B1), in
/// BuildingId order; every other Building is left as it was.
/// </summary>
public class BuildingRebuildTests
{
    /// <summary>The store's whole stage 2 cost, as in <c>game/data/buildings.json</c>: 50 wood, 25 stone, 300 gold.</summary>
    private static readonly (MaterialId, int)[] Stage2Items = [(MaterialId.Wood, 50), (MaterialId.Stone, 25)];

    private const int Stage2Gold = 300;

    private static Building Others(BuildingId id) => new(id, 0);

    // ---- AC1 / BR-B3: the full cost in → one stage up, deliveries cleared ----

    [Fact]
    public void AC1_With_the_full_stage_2_cost_delivered_the_store_rises_to_stage_2_and_its_deliveries_are_cleared()
    {
        var game = Store(storeGold: Stage2Gold, delivered: Stage2Items);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Equal([BuildingId.GeneralStore], rebuilt);
        var store = StoreOf(game);
        Assert.Equal(2, store.Stage);
        Assert.Empty(store.Delivered);
        Assert.Equal(0, store.DeliveredGold);
    }

    [Fact]
    public void AC1_The_full_cost_delivered_through_Deliver_and_DeliverGold_rebuilds_the_store()
    {
        var game = Store(
            gold: 400,
            held: [(Material(MaterialId.Wood), 50), (Material(MaterialId.Stone), 25)]);
        Assert.True(game.Deliver(BuildingId.GeneralStore, MaterialId.Wood, 50, StoreStages).IsOk);
        Assert.True(game.Deliver(BuildingId.GeneralStore, MaterialId.Stone, 25, StoreStages).IsOk);
        Assert.True(game.DeliverGold(BuildingId.GeneralStore, 300, StoreStages).IsOk);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Equal([BuildingId.GeneralStore], rebuilt);
        Assert.Equal("GeneralStore:2:-:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B3_In_the_Night_after_the_next_stage_is_fully_delivered_the_stage_rises_by_one_and_deliveries_clear()
    {
        var game = Store(storeGold: Stage2Gold, delivered: Stage2Items);
        Assert.True(StoreOf(game).RebuildDue(StoreStages));

        game.RebuildDue(StoreStages);

        Assert.Equal("GeneralStore:2:-:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B3_After_a_rebuild_the_next_stage_is_stage_3_and_nothing_is_due()
    {
        var game = Store(storeGold: Stage2Gold, delivered: Stage2Items);

        game.RebuildDue(StoreStages);

        var store = StoreOf(game);
        Assert.Equal(3, store.NextStage(StoreStages)?.Stage);
        Assert.False(store.RebuildDue(StoreStages));
    }

    // ---- AC2: part of the cost in → nothing changes ----

    [Fact]
    public void AC2_With_every_item_but_none_of_the_gold_the_store_stays_at_stage_1_with_its_deliveries()
    {
        var game = Store(storeGold: 0, delivered: Stage2Items);
        var before = Describe(game.Buildings);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Empty(rebuilt);
        Assert.Equal(before, Describe(game.Buildings));
        Assert.Equal("GeneralStore:1:Wood=50;Stone=25:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void AC2_With_the_gold_but_one_stone_short_the_store_stays_at_stage_1_with_its_deliveries()
    {
        var game = Store(storeGold: Stage2Gold, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 24)]);
        var before = Describe(game.Buildings);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Empty(rebuilt);
        Assert.Equal(before, Describe(game.Buildings));
        Assert.Equal("GeneralStore:1:Wood=50;Stone=24:300", Describe(StoreOf(game)));
    }

    [Fact]
    public void AC2_With_the_gold_one_short_the_store_stays_at_stage_1_with_its_deliveries()
    {
        var game = Store(storeGold: Stage2Gold - 1, delivered: Stage2Items);
        var before = Describe(game.Buildings);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Empty(rebuilt);
        Assert.Equal(before, Describe(game.Buildings));
    }

    // ---- AC3 / BR-B1: one stage per Night, never past the last row ----

    [Fact]
    public void AC3_With_the_stage_2_and_stage_3_costs_both_covered_one_Night_raises_the_store_only_to_stage_2()
    {
        // Over-delivered so the stage 2 cost (50 wood, 25 stone, 300 gold) and the stage 3 cost (40 plank, 60 stone,
        // 1200 gold) are both covered at once.
        var game = WithBuildings(
        [
            new Building(
                BuildingId.GeneralStore,
                1,
                new Dictionary<MaterialId, int>
                {
                    [MaterialId.Wood] = 50, [MaterialId.Stone] = 85, [MaterialId.Plank] = 40,
                },
                1500),
            Others(BuildingId.Carpenter),
            Others(BuildingId.MirasHouse),
            Others(BuildingId.Blacksmith),
        ]);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Equal([BuildingId.GeneralStore], rebuilt);
        Assert.Equal(2, StoreOf(game).Stage);
    }

    [Fact]
    public void BR_B1_A_Building_rises_one_stage_at_a_time_so_a_second_rebuild_step_with_nothing_new_delivered_does_nothing()
    {
        var game = Store(storeGold: 5000, delivered: [(MaterialId.Wood, 500), (MaterialId.Stone, 500), (MaterialId.Plank, 500)]);

        var first = game.RebuildDue(StoreStages);
        var second = game.RebuildDue(StoreStages);

        Assert.Equal([BuildingId.GeneralStore], first);
        Assert.Empty(second);
        Assert.Equal("GeneralStore:2:-:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void BR_B1_A_store_at_stage_4_with_no_stage_5_row_is_never_raised()
    {
        var game = WithBuildings(
        [
            new Building(
                BuildingId.GeneralStore,
                4,
                new Dictionary<MaterialId, int>
                {
                    [MaterialId.Plank] = 999, [MaterialId.Stone] = 999, [MaterialId.RoskOre] = 999,
                },
                9999),
            Others(BuildingId.Carpenter),
            Others(BuildingId.MirasHouse),
            Others(BuildingId.Blacksmith),
        ]);
        var before = Describe(game.Buildings);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Empty(rebuilt);
        Assert.Equal(before, Describe(game.Buildings));
    }

    // ---- edge: several Buildings due in one Night ----

    [Fact]
    public void AC1_Several_Buildings_due_in_one_Night_all_rise_in_BuildingId_order_and_the_others_are_unchanged()
    {
        IReadOnlyList<BuildingStage> stages =
        [
            new(BuildingId.Carpenter, 1, [new(MaterialId.Wood, 10)], new Gold(50), "The carpenter reopens"),
            .. StoreStages,
        ];
        var game = WithBuildings(
        [
            new Building(
                BuildingId.GeneralStore,
                1,
                new Dictionary<MaterialId, int> { [MaterialId.Wood] = 50, [MaterialId.Stone] = 25 },
                300),
            new Building(BuildingId.Carpenter, 0, new Dictionary<MaterialId, int> { [MaterialId.Wood] = 10 }, 50),
            new Building(BuildingId.MirasHouse, 0, new Dictionary<MaterialId, int> { [MaterialId.Wood] = 7 }, 20),
            Others(BuildingId.Blacksmith),
        ]);

        var rebuilt = game.RebuildDue(stages);

        Assert.Equal([BuildingId.GeneralStore, BuildingId.Carpenter], rebuilt);
        Assert.Equal(
            "GeneralStore:2:-:0,Carpenter:1:-:0,MirasHouse:0:Wood=7:20,Blacksmith:0:-:0",
            Describe(game.Buildings));
    }

    [Fact]
    public void AC2_A_Night_with_nothing_fully_delivered_changes_no_Building()
    {
        var game = Store(storeGold: 120, delivered: [(MaterialId.Wood, 30)]);
        var before = Describe(game.Buildings);

        var rebuilt = game.RebuildDue(StoreStages);

        Assert.Empty(rebuilt);
        Assert.Equal(before, Describe(game.Buildings));
    }
}
