using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>The Night (<see cref="NightProcessor.Run"/>): growth (BR-T4, BR-T5), watering reset (BR-T7, BR-T8), then
/// the next morning's energy and clock (BR-P3).</summary>
public class NightTests
{
    private static readonly GridPosition Spot = new(3, 2);
    private static readonly GridPosition Other = new(4, 2);
    private static readonly GridPosition Grass = new(0, 0);

    private static void Night(GameState game) => NightProcessor.Run(game, TestGames.Shipped, DayEndReason.Slept);

    private static void WaterWithCan(GameState game, GridPosition position) =>
        Assert.True(game.Water(position, TestGames.Shipped.Tuning.ToolEnergyCost).IsOk);

    [Fact]
    public void AC4_a_growing_planting_on_a_watered_tile_gains_exactly_one_day()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 1), watered: true));

        Night(game);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 2), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void BR_T4_a_growing_planting_on_a_watered_tile_grows_one_day_overnight()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Moonmelon, 4), watered: true);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Moonmelon));

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Moonmelon, 5), tile.Planting);
    }

    [Fact]
    public void BR_T4_planting_on_unwatered_tile_does_not_grow()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Moonmelon, 4), watered: false);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Moonmelon));

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Moonmelon, 4), tile.Planting);
    }

    [Fact]
    public void AC5_a_growing_planting_on_an_unwatered_tile_is_unchanged_after_the_night_and_still_there()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: false));

        Night(game);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void BR_T5_no_planting_dies_or_disappears_in_the_night()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(new GridPosition(1, 1), TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: false),
            TestGames.TilledWith(new GridPosition(2, 1), TestGames.PlantingOf(CropTypeId.Dusktuber, 3), watered: true),
            TestGames.TilledWith(new GridPosition(3, 1), TestGames.Ripe(CropTypeId.Moonmelon), watered: true),
            TestGames.TilledWith(new GridPosition(4, 1), TestGames.Ripe(CropTypeId.Emberroot), watered: false));

        for (var night = 0; night < 5; night++)
        {
            Night(game);
        }

        Assert.Equal(4, game.FarmPlot.Tiles.Count(tile => tile.Planting is not null));
        Assert.All(
            new[] { new GridPosition(1, 1), new GridPosition(2, 1), new GridPosition(3, 1), new GridPosition(4, 1) },
            position => Assert.NotNull(game.FarmPlot[position].Planting));
    }

    [Fact]
    public void AC6_emberroot_watered_before_each_of_three_nights_is_ripe_after_the_third()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0)));

        for (var night = 0; night < 3; night++)
        {
            WaterWithCan(game, Spot);
            Night(game);
        }

        var planting = game.FarmPlot[Spot].Planting!;
        Assert.Equal(PlantingStatus.Ripe, planting.StatusFor(TestGames.Crop(CropTypeId.Emberroot)));
        Assert.Equal(3, planting.DaysGrown);
    }

    [Fact]
    public void AC6_emberroot_watered_before_only_two_of_four_nights_is_growing_with_two_days()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0)));
        var watered = new[] { true, false, true, false };

        foreach (var water in watered)
        {
            if (water)
            {
                WaterWithCan(game, Spot);
            }

            Night(game);
        }

        var planting = game.FarmPlot[Spot].Planting!;
        Assert.Equal(PlantingStatus.Growing, planting.StatusFor(TestGames.Crop(CropTypeId.Emberroot)));
        Assert.Equal(2, planting.DaysGrown);
    }

    [Fact]
    public void AC7_a_ripe_planting_on_a_watered_tile_is_unchanged_by_the_night()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.Ripe(CropTypeId.Emberroot), watered: true));

        Night(game);

        Assert.Equal(TestGames.Ripe(CropTypeId.Emberroot), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void AC7_grow_overnight_never_takes_a_ripe_planting_past_grow_days()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.Ripe(CropTypeId.Dusktuber), watered: true);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Dusktuber));

        Assert.Equal(TestGames.Crop(CropTypeId.Dusktuber).GrowDays, tile.Planting!.DaysGrown);
    }

    [Fact]
    public void AC8_after_the_night_no_tile_is_watered_tilled_stays_tilled_and_grass_stays_grass()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true),
            TestGames.TilledWith(Other, null, watered: true),
            TestGames.TilledWith(new GridPosition(5, 2), null, watered: false));
        var tilledBefore = game.FarmPlot.Tiles.Where(tile => tile.State == TileState.Tilled).Select(tile => tile.Position).ToList();
        var grassBefore = game.FarmPlot.Tiles.Where(tile => tile.State == TileState.Grass).Select(tile => tile.Position).ToList();

        Night(game);

        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
        Assert.All(tilledBefore, position => Assert.Equal(TileState.Tilled, game.FarmPlot[position].State));
        Assert.All(grassBefore, position => Assert.Equal(TileState.Grass, game.FarmPlot[position].State));
    }

    [Fact]
    public void BR_T7_every_tile_is_dry_after_the_night()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: true),
            TestGames.TilledWith(Other, TestGames.Ripe(CropTypeId.Moonmelon), watered: true));

        game.DryAllTiles();

        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
    }

    [Fact]
    public void BR_T7_dry_clears_the_watered_flag_and_keeps_the_planting()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: true);

        tile.Dry();

        Assert.False(tile.Watered);
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), tile.Planting);
    }

    [Fact]
    public void BR_T8_a_tilled_tile_never_reverts_to_grass_over_the_night()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: false),
            TestGames.TilledWith(Other, null, watered: true));

        Night(game);
        Night(game);

        Assert.Equal(TileState.Tilled, game.FarmPlot[Spot].State);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Other].State);
        Assert.Equal(TileState.Grass, game.FarmPlot[Grass].State);
    }

    [Fact]
    public void AC9_growth_runs_before_the_watering_reset_then_energy_and_clock_are_set()
    {
        var game = TestGames.WithTiles(
            20, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 2), watered: true));
        game.AdvanceClock(600, TestGames.Shipped.Tuning.DayEndMinutes);

        Night(game);

        // Grew although no Tile is watered afterwards: growth read the watered flag before the reset cleared it.
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 3), game.FarmPlot[Spot].Planting);
        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
        Assert.Equal(TestGames.Shipped.Tuning.MaxEnergy, game.Player.Energy);
        Assert.Equal(TestGames.Shipped.Tuning.DayStartMinutes, game.ClockMinutes);
        Assert.Equal(DayEndReason.Slept, game.LastDayEnd);
    }

    [Fact]
    public void Planting_with_unknown_crop_type_is_unchanged()
    {
        // Sunbell is a CropTypeId with no data row yet (REQ-42).
        var sunbell = TestGames.PlantingOf(CropTypeId.Sunbell, 1);
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, sunbell, watered: true));

        Night(game);

        Assert.Equal(sunbell, game.FarmPlot[Spot].Planting);
        Assert.False(game.FarmPlot[Spot].Watered);
    }

    [Fact]
    public void Grow_plantings_leaves_a_planting_with_no_data_row_unchanged_and_grows_the_rest()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Starcorn, 0), watered: true),
            TestGames.TilledWith(Other, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true));

        game.GrowPlantings(TestGames.Shipped.CropTypes);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Starcorn, 0), game.FarmPlot[Spot].Planting);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), game.FarmPlot[Other].Planting);
    }
}
