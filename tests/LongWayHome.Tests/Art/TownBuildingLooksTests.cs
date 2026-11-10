using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>#165 AC5: the general store's look follows its stage, and stages 3 and 4 have the storeroom drum.</summary>
public class TownBuildingLooksTests
{
    [Theory]
    [InlineData(1, StoreStage.MaggiesCorner)]
    [InlineData(2, StoreStage.Reopened)]
    [InlineData(3, StoreStage.Storeroom)]
    [InlineData(4, StoreStage.ExpandedShop)]
    public void AC5_store_stages_1_to_4_map_to_their_looks(int stage, StoreStage expected) =>
        Assert.Equal(expected, TownBuildingLooks.Store(stage));

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void AC5_a_store_stage_below_1_or_above_4_throws(int stage) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TownBuildingLooks.Store(stage));

    [Theory]
    [InlineData(StoreStage.MaggiesCorner, false)]
    [InlineData(StoreStage.Reopened, false)]
    [InlineData(StoreStage.Storeroom, true)]
    [InlineData(StoreStage.ExpandedShop, true)]
    public void AC7_only_the_Storeroom_and_ExpandedShop_looks_have_the_storeroom_drum(StoreStage stage, bool expected) =>
        Assert.Equal(expected, TownBuildingLooks.HasStoreroom(stage));

    [Fact]
    public void AC7_the_storeroom_drum_sits_at_the_art_offset_with_radius_1_7_and_height_2_3()
    {
        Assert.Equal((4.35f, -0.6f, 1.7f, 2.3f), (
            TownBuildingLooks.StoreroomOffsetX,
            TownBuildingLooks.StoreroomOffsetZ,
            TownBuildingLooks.StoreroomRadius,
            TownBuildingLooks.StoreroomHeight));
    }
}
