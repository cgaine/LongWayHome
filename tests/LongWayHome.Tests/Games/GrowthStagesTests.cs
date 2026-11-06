using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Games;

/// <summary>The growth stage a Planting is drawn as: <see cref="GrowthStages.For"/>.</summary>
public class GrowthStagesTests
{
    public static TheoryData<CropTypeId, int, GrowthStage> ShippedStages => new()
    {
        { CropTypeId.Emberroot, 0, GrowthStage.Seed },
        { CropTypeId.Emberroot, 1, GrowthStage.Sprout },
        { CropTypeId.Emberroot, 2, GrowthStage.Young },
        { CropTypeId.Emberroot, 3, GrowthStage.Ripe },
        { CropTypeId.Dusktuber, 0, GrowthStage.Seed },
        { CropTypeId.Dusktuber, 1, GrowthStage.Sprout },
        { CropTypeId.Dusktuber, 2, GrowthStage.Sprout },
        { CropTypeId.Dusktuber, 3, GrowthStage.Young },
        { CropTypeId.Dusktuber, 4, GrowthStage.Young },
        { CropTypeId.Dusktuber, 5, GrowthStage.Ripe },
        { CropTypeId.Moonmelon, 0, GrowthStage.Seed },
        { CropTypeId.Moonmelon, 1, GrowthStage.Sprout },
        { CropTypeId.Moonmelon, 2, GrowthStage.Sprout },
        { CropTypeId.Moonmelon, 3, GrowthStage.Sprout },
        { CropTypeId.Moonmelon, 4, GrowthStage.Young },
        { CropTypeId.Moonmelon, 5, GrowthStage.Young },
        { CropTypeId.Moonmelon, 6, GrowthStage.Young },
        { CropTypeId.Moonmelon, 7, GrowthStage.Young },
        { CropTypeId.Moonmelon, 8, GrowthStage.Ripe },
    };

    public static TheoryData<CropTypeId> ShippedCrops => [.. TestGames.Shipped.CropTypes.Keys];

    [Theory]
    [MemberData(nameof(ShippedStages))]
    public void AC1_stage_for_days_grown_follows_the_shipped_grow_days(
        CropTypeId crop,
        int daysGrown,
        GrowthStage expected)
    {
        var planting = TestGames.PlantingOf(crop, daysGrown);

        var stage = GrowthStages.For(planting, TestGames.Crop(crop));

        Assert.Equal(expected, stage);
    }

    [Theory]
    [MemberData(nameof(ShippedCrops))]
    public void AC2_stage_is_Ripe_exactly_when_the_planting_status_is_Ripe(CropTypeId crop)
    {
        var cropType = TestGames.Crop(crop);

        for (var daysGrown = 0; daysGrown <= cropType.GrowDays; daysGrown++)
        {
            var planting = TestGames.PlantingOf(crop, daysGrown);

            var stage = GrowthStages.For(planting, cropType);

            Assert.Equal(
                planting.StatusFor(cropType) == PlantingStatus.Ripe,
                stage == GrowthStage.Ripe);
        }
    }

    [Fact]
    public void Stage_for_a_crop_type_that_is_not_the_plantings_throws_ArgumentException()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, 1);

        Assert.Throws<ArgumentException>(() => GrowthStages.For(planting, TestGames.Crop(CropTypeId.Dusktuber)));
    }
}
