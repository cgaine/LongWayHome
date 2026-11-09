using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #163: <c>GameSession.RebuildSign</c>, <c>Deliver</c>, <c>DeliverGold</c> and <c>BuildingsChanged</c> — town only,
/// 100 gold per press, all held items up to the need (BR-B2, BR-B4, BR-P5).
/// </summary>
public class RebuildSessionTests
{
    private static List<string> RecordEvents(GameSession session)
    {
        var events = new List<string>();
        session.PlayerChanged += () => events.Add("PlayerChanged");
        session.InventoryChanged += () => events.Add("InventoryChanged");
        session.BuildingsChanged += () => events.Add("BuildingsChanged");
        return events;
    }

    private static (MaterialId? Material, string Label, int Delivered, int Needed, int Held) Line(RebuildLine line) =>
        (line.Material, line.Label, line.Delivered, line.Needed, line.Held);

    // ---- AC5 / BR-P5: gold 100 at a time ----

    [Fact]
    public void AC5_With_120_gold_DeliverGold_takes_100_then_20_then_is_NothingToDeliver()
    {
        var session = InTown(Store(gold: 120));

        var first = session.DeliverGold(BuildingId.GeneralStore);
        var goldAfterFirst = session.Game.Player.Gold.Value;
        var second = session.DeliverGold(BuildingId.GeneralStore);
        var goldAfterSecond = session.Game.Player.Gold.Value;
        var third = session.DeliverGold(BuildingId.GeneralStore);

        Assert.Equal(DeliveryResult.Ok(100), first);
        Assert.Equal(20, goldAfterFirst);
        Assert.Equal(DeliveryResult.Ok(20), second);
        Assert.Equal(0, goldAfterSecond);
        Assert.Equal(DeliveryResult.Rejected(Rejection.NothingToDeliver), third);
        Assert.Equal(0, session.Game.Player.Gold.Value);
        Assert.Equal("GeneralStore:1:-:120", Describe(StoreOf(session.Game)));
    }

    [Fact]
    public void BR_P5_DeliverGold_takes_only_the_gold_held_and_leaves_0_never_less()
    {
        var session = InTown(Store(gold: 30));

        var result = session.DeliverGold(BuildingId.GeneralStore);

        Assert.Equal(DeliveryResult.Ok(30), result);
        Assert.Equal(new Gold(0), session.Game.Player.Gold);
    }

    [Fact]
    public void DeliverGold_takes_the_remaining_need_when_it_is_under_100()
    {
        var session = InTown(Store(gold: 500, storeGold: 260));

        var result = session.DeliverGold(BuildingId.GeneralStore);

        Assert.Equal(DeliveryResult.Ok(40), result);
        Assert.Equal(460, session.Game.Player.Gold.Value);
    }

    // ---- Deliver: all held, up to the need ----

    [Fact]
    public void Deliver_hands_over_every_one_held_up_to_what_is_needed()
    {
        var session = InTown(Store(held: [(Material(MaterialId.Wood), 60)]));

        var result = session.Deliver(BuildingId.GeneralStore, MaterialId.Wood);

        Assert.Equal(DeliveryResult.Ok(50), result);
        Assert.Equal(10, Held(session.Game, MaterialId.Wood));
        Assert.Equal("GeneralStore:1:Wood=50:0", Describe(StoreOf(session.Game)));
    }

    [Fact]
    public void Deliver_with_fewer_held_than_needed_hands_over_all_held()
    {
        var session = InTown(Store(held: [(Material(MaterialId.Stone), 8)]));

        var result = session.Deliver(BuildingId.GeneralStore, MaterialId.Stone);

        Assert.Equal(DeliveryResult.Ok(8), result);
        Assert.Equal(0, Held(session.Game, MaterialId.Stone));
    }

    // ---- Town only ----

    [Fact]
    public void Deliveries_outside_town_are_NotOffered_and_change_nothing()
    {
        var game = Store(gold: 500, held: [(Material(MaterialId.Wood), 30)]);
        var session = new GameSession(game, BuildingGames.Data);
        var before = Snapshot(game);

        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), session.Deliver(BuildingId.GeneralStore, MaterialId.Wood));
        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), session.DeliverGold(BuildingId.GeneralStore));
        Assert.Equal(before, Snapshot(game));
    }

    // ---- Events ----

    [Fact]
    public void A_delivery_raises_PlayerChanged_InventoryChanged_and_BuildingsChanged()
    {
        var session = InTown(Store(gold: 500, held: [(Material(MaterialId.Wood), 30)]));
        var events = RecordEvents(session);

        session.Deliver(BuildingId.GeneralStore, MaterialId.Wood);
        session.DeliverGold(BuildingId.GeneralStore);

        // Each successful delivery raises each event once; the order between them isn't specified.
        Assert.Equal(
            ["BuildingsChanged", "BuildingsChanged", "InventoryChanged", "InventoryChanged", "PlayerChanged", "PlayerChanged"],
            events.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_refused_delivery_raises_no_event()
    {
        var session = InTown(Store(gold: 0));
        var events = RecordEvents(session);

        Assert.False(session.Deliver(BuildingId.GeneralStore, MaterialId.Wood).IsOk);
        Assert.False(session.DeliverGold(BuildingId.GeneralStore).IsOk);
        Assert.False(session.Deliver(BuildingId.Carpenter, MaterialId.Wood).IsOk);

        Assert.Empty(events);
    }

    // ---- RebuildSign ----

    [Fact]
    public void RebuildSign_lists_each_cost_line_then_gold_with_delivered_needed_and_held()
    {
        var session = InTown(Store(gold: 120, delivered: [(MaterialId.Wood, 12)], held: [(Material(MaterialId.Wood), 8)]));

        var sign = session.RebuildSign(BuildingId.GeneralStore);

        Assert.NotNull(sign);
        Assert.Equal(BuildingId.GeneralStore, sign.Building);
        Assert.Equal(2, sign.Stage);
        Assert.Equal(Stage2Unlocks, sign.Unlocks);
        Assert.Equal(
            [
                (MaterialId.Wood, "Wood", 12, 50, 8),
                (MaterialId.Stone, "Stone", 0, 25, 0),
                ((MaterialId?)null, "Gold", 0, 300, 120),
            ],
            sign.Lines.Select(Line));
    }

    [Fact]
    public void RebuildSign_for_the_store_at_stage_3_shows_its_stage_4_lines_and_unlocks()
    {
        var session = InTown(Store(storeStage: 3, held: [(Material(MaterialId.Plank), 5)]));

        var sign = session.RebuildSign(BuildingId.GeneralStore);

        Assert.NotNull(sign);
        Assert.Equal(4, sign.Stage);
        Assert.Equal(Stage4Unlocks, sign.Unlocks);
        Assert.Equal(
            ["Plank:0/80:5", "Stone:0/120:0", "Rosk ore:0/20:0", "Gold:0/3000:100"],
            sign.Lines.Select(line => $"{line.Label}:{line.Delivered}/{line.Needed}:{line.Held}"));
    }

    [Fact]
    public void BR_B4_RebuildSign_is_null_for_a_building_with_no_next_stage()
    {
        var session = InTown(Store());

        Assert.Null(session.RebuildSign(BuildingId.Carpenter));
        Assert.Null(session.RebuildSign(BuildingId.MirasHouse));
        Assert.Null(session.RebuildSign(BuildingId.Blacksmith));
    }

    [Fact]
    public void BR_B4_RebuildSign_is_null_for_the_store_at_its_last_stage()
    {
        var session = InTown(Store(storeStage: 4));

        Assert.Null(session.RebuildSign(BuildingId.GeneralStore));
    }

    [Fact]
    public void BR_B4_A_new_games_store_has_a_rebuild_sign()
    {
        var session = InTown(BuildingGames.WithBuildings(null));

        Assert.NotNull(session.RebuildSign(BuildingId.GeneralStore));
    }

    // ---- AC11: complete, but the stage rises only in the Night ----

    [Fact]
    public void AC11_Delivering_the_full_stage_2_cost_leaves_the_store_at_stage_1_and_the_sign_up_with_every_line_complete()
    {
        var session = InTown(Store(
            gold: 300, held: [(Material(MaterialId.Wood), 50), (Material(MaterialId.Stone), 25)]));

        Assert.Equal(DeliveryResult.Ok(50), session.Deliver(BuildingId.GeneralStore, MaterialId.Wood));
        Assert.Equal(DeliveryResult.Ok(25), session.Deliver(BuildingId.GeneralStore, MaterialId.Stone));
        Assert.Equal(DeliveryResult.Ok(100), session.DeliverGold(BuildingId.GeneralStore));
        Assert.Equal(DeliveryResult.Ok(100), session.DeliverGold(BuildingId.GeneralStore));
        Assert.Equal(DeliveryResult.Ok(100), session.DeliverGold(BuildingId.GeneralStore));

        Assert.Equal(1, session.Game.BuildingStage(BuildingId.GeneralStore));
        Assert.True(StoreOf(session.Game).RebuildDue(StoreStages));
        var sign = session.RebuildSign(BuildingId.GeneralStore);
        Assert.NotNull(sign);
        Assert.Equal(2, sign.Stage);
        Assert.All(sign.Lines, line => Assert.True(line.Complete, $"{line.Label} is not complete"));
        Assert.True(sign.AllComplete);
        Assert.True(sign.WoodPip && sign.StonePip && sign.GoldPip, "a pip is still empty");
    }

    [Fact]
    public void AC11_Every_line_complete_refuses_a_further_delivery_as_AlreadyDelivered()
    {
        var session = InTown(Store(
            gold: 100, storeGold: 300, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 25)],
            held: [(Material(MaterialId.Wood), 5)]));

        Assert.Equal(
            DeliveryResult.Rejected(Rejection.AlreadyDelivered), session.Deliver(BuildingId.GeneralStore, MaterialId.Wood));
        Assert.Equal(DeliveryResult.Rejected(Rejection.AlreadyDelivered), session.DeliverGold(BuildingId.GeneralStore));
        Assert.Equal(1, session.Game.BuildingStage(BuildingId.GeneralStore));
    }
}
