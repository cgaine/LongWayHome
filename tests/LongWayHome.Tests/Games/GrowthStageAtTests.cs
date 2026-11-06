using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// The stage a Tile's Planting is drawn as (<see cref="GameSession.GrowthStageAt"/>), the canonical crop labels
/// (<see cref="CropLabels"/>) and the prompt's guards for an inconsistent preview.
/// </summary>
public class GrowthStageAtTests
{
    [Fact]
    public void Stage_at_a_tile_follows_the_crop_data_and_is_null_with_no_planting()
    {
        var seedAt = new GridPosition(0, 0);
        var youngAt = new GridPosition(1, 0);
        var ripeAt = new GridPosition(2, 0);
        var emptyAt = new GridPosition(3, 0);
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(seedAt, TestGames.PlantingOf(CropTypeId.Emberroot, 0)),
            TestGames.TilledWith(youngAt, TestGames.PlantingOf(CropTypeId.Moonmelon, 4)),
            TestGames.TilledWith(ripeAt, TestGames.Ripe(CropTypeId.Dusktuber)),
            TestGames.TilledWith(emptyAt, null));

        Assert.Equal(GrowthStage.Seed, session.GrowthStageAt(seedAt));
        Assert.Equal(GrowthStage.Young, session.GrowthStageAt(youngAt));
        Assert.Equal(GrowthStage.Ripe, session.GrowthStageAt(ripeAt));
        Assert.Null(session.GrowthStageAt(emptyAt));
        Assert.Null(session.GrowthStageAt(new GridPosition(4, 0)));
    }

    [Theory]
    [InlineData(0, GrowthStage.Seed)]
    [InlineData(1, GrowthStage.Sprout)]
    [InlineData(8, GrowthStage.Sprout)]
    [InlineData(40, GrowthStage.Sprout)]
    public void Stage_of_a_crop_with_no_data_row_is_Seed_then_Sprout_and_never_Ripe(int daysGrown, GrowthStage expected)
    {
        var at = new GridPosition(0, 0);
        var session = new GameSession(
            TestGames.WithTiles(150, null, TestGames.TilledWith(at, TestGames.PlantingOf(CropTypeId.Sunbell, daysGrown))),
            TestGames.ShippedWithout(CropTypeId.Sunbell));

        Assert.Equal(expected, session.GrowthStageAt(at));
        Assert.Equal(PlantingStatus.Growing, session.PlantingStatusAt(at));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, "Emberroot")]
    [InlineData(CropTypeId.Dusktuber, "Dusktuber")]
    [InlineData(CropTypeId.Moonmelon, "Moonmelon")]
    [InlineData(CropTypeId.Sunbell, "Sunbell")]
    [InlineData(CropTypeId.Starcorn, "Starcorn")]
    public void Crop_label_is_the_canonical_display_label(CropTypeId crop, string expected) =>
        Assert.Equal(expected, CropLabels.For(crop));

    [Fact]
    public void Crop_label_of_an_unknown_crop_type_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CropLabels.For((CropTypeId)999));

    [Theory]
    [MemberData(nameof(ShippedCrops))]
    public void Shipped_crop_names_match_the_canonical_labels(CropTypeId crop) =>
        Assert.Equal(CropLabels.For(crop), TestGames.Crop(crop).Name);

    public static TheoryData<CropTypeId> ShippedCrops => [.. TestGames.Shipped.CropTypes.Keys];

    [Fact]
    public void Plant_preview_with_no_seed_held_shows_no_prompt() =>
        Assert.Null(UsePromptText.For(
            new UsePreview(UseActionKind.Plant, null),
            HeldItem.Of(ToolId.Hoe),
            null,
            TestGames.Shipped.CropTypes));

    [Fact]
    public void Harvest_preview_with_no_target_planting_shows_no_prompt() =>
        Assert.Null(UsePromptText.For(
            new UsePreview(UseActionKind.Harvest, null),
            HeldItem.Of(ToolId.Hoe),
            null,
            TestGames.Shipped.CropTypes));
}
