using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Games;
using LongWayHome.Tests.Game;
using LongWayHome.Tests.Saves;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #165 AC4: the Night's rebuild step (BR-B3) runs straight after the shipping payout and before the dropped items
/// are cleared, the save after it holds the new stage (BR-G5), and <see cref="NightResult"/> carries the rebuilt ids.
/// </summary>
public class NightRebuildTests
{
    private static readonly (MaterialId, int)[] Stage2Items = [(MaterialId.Wood, 50), (MaterialId.Stone, 25)];

    private static string NightSource() => SceneText.Code(
        SceneText.ReadRepoFile("src", "LongWayHome.Application", "Games", "NightProcessor.cs"));

    [Fact]
    public void AC4_night_processor_rebuilds_between_the_shipping_payout_and_the_dropped_items_clear()
    {
        // Source order in NightProcessor.Run: the payout, the rebuild and the clear can't be told apart from outside,
        // so the order of the calls is checked directly.
        var raw = SceneText.ReadRepoFile("src", "LongWayHome.Application", "Games", "NightProcessor.cs");
        var source = NightSource();

        var payout = source.IndexOf("game.PayOutShippingBin(", StringComparison.Ordinal);
        var rebuild = source.IndexOf("game.RebuildDue(data.BuildingStages)", StringComparison.Ordinal);
        var clear = source.IndexOf("game.ClearDroppedItems()", StringComparison.Ordinal);
        var save = source.IndexOf("save(game)", StringComparison.Ordinal);

        Assert.True(rebuild >= 0, "NightProcessor never calls game.RebuildDue(data.BuildingStages)");
        Assert.True(payout < rebuild, "RebuildDue must come after PayOutShippingBin");
        Assert.True(rebuild < clear, "RebuildDue must come before ClearDroppedItems");
        Assert.True(clear < save, "the save must stay the last step");

        // The Phase 2 slots for BR-O2 orders and BR-R2/BR-R3 residents sit between the rebuilds and the clear.
        var rawRebuild = raw.IndexOf("game.RebuildDue(data.BuildingStages)", StringComparison.Ordinal);
        var rawClear = raw.IndexOf("game.ClearDroppedItems()", StringComparison.Ordinal);
        var between = raw[rawRebuild..rawClear];
        Assert.Contains("BR-O2", between);
        Assert.Contains("BR-R2", between);
    }

    [Fact]
    public void BR_G5_the_save_after_a_rebuilding_Night_sees_the_store_at_stage_2_and_the_result_carries_it()
    {
        var game = Store(storeGold: 300, delivered: Stage2Items);
        int? stageAtSave = null;
        string? storeAtSave = null;

        var result = NightProcessor.Run(game, BuildingGames.Data, DayEndReason.Slept, new FixedRandom(99), saved =>
        {
            stageAtSave = StoreOf(saved).Stage;
            storeAtSave = Describe(StoreOf(saved));
        });

        Assert.Equal(2, stageAtSave);
        Assert.Equal("GeneralStore:2:-:0", storeAtSave);
        Assert.Equal([BuildingId.GeneralStore], result.Rebuilt);
    }

    [Fact]
    public void AC4_a_Night_that_ends_by_passing_out_rebuilds_too()
    {
        var game = Store(storeGold: 300, delivered: Stage2Items);

        var result = NightProcessor.Run(game, BuildingGames.Data, DayEndReason.PassedOut, new FixedRandom(99), static _ => { });

        Assert.Equal([BuildingId.GeneralStore], result.Rebuilt);
        Assert.Equal("GeneralStore:2:-:0", Describe(StoreOf(game)));
    }

    [Fact]
    public void AC4_the_shorter_Run_overloads_rebuild_and_return_the_ids_too()
    {
        var slept = Store(storeGold: 300, delivered: Stage2Items);
        var rolled = Store(storeGold: 300, delivered: Stage2Items);

        var sleptResult = NightProcessor.Run(slept, BuildingGames.Data, DayEndReason.Slept);
        var rolledResult = NightProcessor.Run(rolled, BuildingGames.Data, DayEndReason.Slept, new FixedRandom(99));

        Assert.Equal([BuildingId.GeneralStore], sleptResult.Rebuilt);
        Assert.Equal([BuildingId.GeneralStore], rolledResult.Rebuilt);
        Assert.Equal(2, StoreOf(slept).Stage);
        Assert.Equal(2, StoreOf(rolled).Stage);
    }

    [Fact]
    public void AC2_a_Night_with_nothing_fully_delivered_rebuilds_nothing_and_its_result_is_empty()
    {
        var game = Store(storeGold: 120, delivered: [(MaterialId.Wood, 30)]);
        var before = Describe(game.Buildings);

        var result = NightProcessor.Run(game, BuildingGames.Data, DayEndReason.Slept, new FixedRandom(99), static _ => { });

        Assert.Empty(result.Rebuilt);
        Assert.Equal(before, Describe(game.Buildings));
    }
}
