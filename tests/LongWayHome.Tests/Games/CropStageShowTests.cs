using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Games;

/// <summary>
/// Characterization tests for #90 (AC5–AC7): the stage a Tile's model shows comes from
/// <see cref="GameSession.GrowthStageAt"/>, driven here through real plays of till, plant, water, sleep and harvest.
/// The crop art changes none of this (BR-T4, BR-T5, BR-T6); these pin it.
/// </summary>
public class CropStageShowTests
{
    private static readonly GridPosition Spot = new(3, 2);

    private static GameSession PlantedSession(CropTypeId crop)
    {
        var session = TestGames.SessionWithTiles();
        Assert.True(session.Till(Spot).IsOk);
        Assert.True(session.Plant(Spot, crop).IsOk);
        return session;
    }

    private static void WaterAndSleep(GameSession session)
    {
        Assert.True(session.Water(Spot).IsOk);
        Assert.True(session.Sleep().IsOk);
    }

    [Fact]
    public void BR_T4_emberroot_watered_daily_shows_seed_sprout_young_then_ripe()
    {
        var session = PlantedSession(CropTypeId.Emberroot);
        var shown = new List<GrowthStage?> { session.GrowthStageAt(Spot) };

        for (var day = 0; day < 3; day++)
        {
            WaterAndSleep(session);
            shown.Add(session.GrowthStageAt(Spot));
        }

        Assert.Equal(
            new GrowthStage?[] { GrowthStage.Seed, GrowthStage.Sprout, GrowthStage.Young, GrowthStage.Ripe },
            shown);
    }

    [Fact]
    public void BR_T5_a_day_without_water_leaves_the_same_stage_showing()
    {
        var session = PlantedSession(CropTypeId.Emberroot);
        WaterAndSleep(session);
        Assert.Equal(GrowthStage.Sprout, session.GrowthStageAt(Spot));

        Assert.True(session.Sleep().IsOk);

        Assert.Equal(GrowthStage.Sprout, session.GrowthStageAt(Spot));
        Assert.NotNull(session.Game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void AC6_dusktuber_shows_seed_sprout_sprout_young_young_ripe_over_its_five_days()
    {
        var session = PlantedSession(CropTypeId.Dusktuber);
        var shown = new List<GrowthStage?> { session.GrowthStageAt(Spot) };

        for (var day = 0; day < 5; day++)
        {
            WaterAndSleep(session);
            shown.Add(session.GrowthStageAt(Spot));
        }

        Assert.Equal(
            new GrowthStage?[]
            {
                GrowthStage.Seed, GrowthStage.Sprout, GrowthStage.Sprout, GrowthStage.Young, GrowthStage.Young,
                GrowthStage.Ripe,
            },
            shown);
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, false)]
    [InlineData(CropTypeId.Emberroot, true)]
    [InlineData(CropTypeId.Dusktuber, false)]
    [InlineData(CropTypeId.Dusktuber, true)]
    public void BR_T6_harvesting_a_ripe_planting_removes_its_stage_and_keeps_the_watered_state(
        CropTypeId crop, bool watered)
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Spot, TestGames.Ripe(crop), watered));
        Assert.Equal(GrowthStage.Ripe, session.GrowthStageAt(Spot));

        Assert.True(session.Harvest(Spot).IsOk);

        var tile = session.Game.FarmPlot[Spot];
        Assert.Null(tile.Planting);
        Assert.Null(session.GrowthStageAt(Spot));
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.Equal(watered, tile.Watered);
    }
}
