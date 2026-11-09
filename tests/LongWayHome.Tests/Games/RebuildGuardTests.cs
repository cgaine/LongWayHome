using LongWayHome.Application.Games;
using LongWayHome.Domain.Buildings;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #163: the guards and text paths around the Rebuild sign that the acceptance tests don't reach: the Building
/// constructor's checks, undefined ids, <see cref="BuildingCodes"/> and the rest of <see cref="RebuildText"/>.
/// </summary>
public class RebuildGuardTests
{
    // ---- Building ----

    [Fact]
    public void A_building_refuses_a_negative_stage_or_gold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Building(BuildingId.Carpenter, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Building(BuildingId.Carpenter, 0, deliveredGold: -1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void A_building_refuses_a_delivered_quantity_below_1(int quantity)
    {
        var delivered = new Dictionary<MaterialId, int> { [MaterialId.Wood] = quantity };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Building(BuildingId.GeneralStore, 1, delivered));
    }

    [Fact]
    public void A_building_refuses_an_undefined_delivered_material()
    {
        var delivered = new Dictionary<MaterialId, int> { [(MaterialId)99] = 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Building(BuildingId.GeneralStore, 1, delivered));
    }

    [Fact]
    public void A_building_keeps_its_delivered_items_in_material_order_and_reads_0_for_one_not_delivered()
    {
        var building = new Building(
            BuildingId.GeneralStore,
            1,
            new Dictionary<MaterialId, int> { [MaterialId.Plank] = 4, [MaterialId.Wood] = 9 });

        Assert.Equal([MaterialId.Wood, MaterialId.Plank], building.Delivered.Keys);
        Assert.Equal(9, building.DeliveredOf(MaterialId.Wood));
        Assert.Equal(0, building.DeliveredOf(MaterialId.Stone));
    }

    // ---- Undefined Building ids ----

    [Fact]
    public void Delivering_to_an_undefined_building_throws_and_changes_nothing()
    {
        var game = Store(gold: 500, held: (Material(MaterialId.Wood), 10));
        var before = Snapshot(game);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.Deliver((BuildingId)99, MaterialId.Wood, 10, StoreStages));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.DeliverGold((BuildingId)99, 100, StoreStages));
        Assert.Equal(before, Snapshot(game));
    }

    [Fact]
    public void RebuildSign_of_an_undefined_building_throws()
    {
        var session = InTown(Store());

        Assert.Throws<ArgumentOutOfRangeException>(() => session.RebuildSign((BuildingId)99));
    }

    [Fact]
    public void A_session_delivery_to_a_building_at_its_last_stage_is_NotOffered()
    {
        var session = InTown(Store(gold: 500, storeStage: 4, held: (Material(MaterialId.Plank), 5)));

        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), session.Deliver(BuildingId.GeneralStore, MaterialId.Plank));
        Assert.Equal(DeliveryResult.Rejected(Rejection.NotOffered), session.DeliverGold(BuildingId.GeneralStore));
    }

    // ---- BuildingCodes ----

    [Theory]
    [InlineData(BuildingId.GeneralStore, "general_store")]
    [InlineData(BuildingId.Carpenter, "carpenter")]
    [InlineData(BuildingId.MirasHouse, "miras_house")]
    [InlineData(BuildingId.Blacksmith, "blacksmith")]
    public void BuildingCodes_round_trip_the_canonical_codes(BuildingId id, string code)
    {
        Assert.Equal(code, BuildingCodes.Code(id));
        Assert.True(BuildingCodes.TryParse(code, out var parsed));
        Assert.Equal(id, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("General_Store")]
    [InlineData("bakery")]
    public void BuildingCodes_refuse_anything_but_an_exact_code(string? code)
    {
        Assert.False(BuildingCodes.TryParse(code, out _));
    }

    [Fact]
    public void BuildingCodes_Code_of_an_undefined_building_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildingCodes.Code((BuildingId)99));
    }

    // ---- RebuildText ----

    [Theory]
    [InlineData(BuildingId.Carpenter, 1, "Rebuild the carpenter — stage 1")]
    [InlineData(BuildingId.MirasHouse, 1, "Rebuild Mira's house — stage 1")]
    [InlineData(BuildingId.Blacksmith, 2, "Rebuild the blacksmith — stage 2")]
    public void The_title_names_each_building(BuildingId building, int stage, string title)
    {
        Assert.Equal(title, RebuildText.Title(building, stage));
    }

    [Fact]
    public void The_title_of_an_undefined_building_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RebuildText.Title((BuildingId)99, 2));
    }

    [Fact]
    public void A_complete_row_reads_delivered_needed_and_the_check_mark()
    {
        Assert.Equal("Gold 300 / 300 ✓", RebuildText.Row(new RebuildLine(null, "Gold", 300, 300, 40)));
    }

    [Fact]
    public void After_a_gold_delivery_the_result_reads_the_gold()
    {
        Assert.Equal("Delivered 100g", RebuildText.AfterDeliver(new RebuildLine(null, "Gold", 100, 300, 20), 100));
        Assert.Equal(
            "Delivered 30 Wood",
            RebuildText.AfterDeliver(new RebuildLine(MaterialId.Wood, "Wood", 30, 50, 0), 30));
    }

    [Fact]
    public void Any_other_refusal_has_a_general_line()
    {
        var general = RebuildText.Rejection(Rejection.NotEnoughGold);

        Assert.False(string.IsNullOrWhiteSpace(general));
        Assert.NotEqual(RebuildText.Rejection(Rejection.NotOffered), general);
    }
}
