using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Buildings;

/// <summary>
/// #165: the paths the spec tests don't drive. <c>Game.RebuildDue</c> refuses null stage rows and leaves a Building
/// with no next stage row alone; a rebuilt Building can be delivered to again for its next stage; the labels refuse an
/// unknown Building and a null id list.
/// </summary>
public class BuildingRebuildGuardTests
{
    [Fact]
    public void RebuildDue_with_null_stage_rows_throws_and_changes_no_Building()
    {
        var game = Store(storeGold: 300, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 25)]);
        var before = Describe(game.Buildings);

        Assert.Throws<ArgumentNullException>(() => game.RebuildDue(null!));

        Assert.Equal(before, Describe(game.Buildings));
    }

    [Fact]
    public void BR_B1_RebuildDue_with_no_stage_rows_at_all_rebuilds_nothing()
    {
        var game = Store(storeGold: 300, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 25)]);
        var before = Describe(game.Buildings);

        Assert.Empty(game.RebuildDue([]));
        Assert.Equal(before, Describe(game.Buildings));
    }

    [Fact]
    public void BR_B3_After_a_rebuild_deliveries_count_toward_the_next_stage_from_0()
    {
        var game = Store(
            gold: 100,
            storeGold: 300,
            delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 25)],
            held: [(Material(MaterialId.Stone), 10)]);
        game.RebuildDue(StoreStages);

        Assert.True(game.Deliver(BuildingId.GeneralStore, MaterialId.Stone, 10, StoreStages).IsOk);

        Assert.Equal("GeneralStore:2:Stone=10:0", Describe(StoreOf(game)));
        Assert.False(StoreOf(game).RebuildDue(StoreStages));
    }

    [Fact]
    public void BuildingLabels_refuse_a_value_that_is_not_a_Building()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildingLabels.Label((BuildingId)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildingLabels.RebuiltLine((BuildingId)99));
    }

    [Fact]
    public void BuildingLabels_RebuiltText_with_null_ids_throws()
    {
        Assert.Throws<ArgumentNullException>(() => BuildingLabels.RebuiltText(null!));
    }
}
