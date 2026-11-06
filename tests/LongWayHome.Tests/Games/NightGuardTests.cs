using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The guards and edge paths of the Night and the day end that the AC tests do not reach.</summary>
public class NightGuardTests
{
    private const int DayStart = 360;
    private const int DayEnd = 1560;

    private static readonly GridPosition Spot = new(3, 2);

    [Fact]
    public void BR_T4_grow_overnight_on_a_watered_tile_with_no_planting_changes_nothing()
    {
        var tile = TestGames.TilledWith(Spot, null, watered: true);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Emberroot));

        Assert.Null(tile.Planting);
        Assert.True(tile.Watered);
    }

    [Fact]
    public void BR_T4_grow_overnight_never_grows_a_planting_restored_past_grow_days()
    {
        var pastRipe = TestGames.PlantingOf(CropTypeId.Emberroot, TestGames.Crop(CropTypeId.Emberroot).GrowDays + 2);
        var tile = TestGames.TilledWith(Spot, pastRipe, watered: true);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Emberroot));

        Assert.Equal(pastRipe, tile.Planting);
    }

    [Fact]
    public void Grow_overnight_with_no_crop_type_throws()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true);

        Assert.Throws<ArgumentNullException>(() => tile.GrowOvernight(null!));
    }

    [Fact]
    public void Grow_overnight_with_another_crop_types_data_throws_and_keeps_the_planting()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true);

        Assert.Throws<ArgumentException>(() => tile.GrowOvernight(TestGames.Crop(CropTypeId.Moonmelon)));
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 0), tile.Planting);
    }

    [Fact]
    public void Grow_plantings_with_no_crop_types_throws()
    {
        var game = TestGames.WithEnergy(150);

        Assert.Throws<ArgumentNullException>(() => game.GrowPlantings(null!));
    }

    [Fact]
    public void BR_P1_start_next_day_with_energy_outside_0_to_150_throws_and_changes_nothing()
    {
        var game = TestGames.WithEnergy(10);
        game.AdvanceClock(500, DayEnd);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.StartNextDay(DayEndReason.Slept, 151, DayStart));

        Assert.Null(game.LastDayEnd);
        Assert.Equal(10, game.Player.Energy);
        Assert.Equal(DayStart + 500, game.ClockMinutes);
    }

    [Fact]
    public void Night_with_no_game_or_no_data_throws()
    {
        Assert.Throws<ArgumentNullException>(() => NightProcessor.Run(null!, TestGames.Shipped, DayEndReason.Slept));
        Assert.Throws<ArgumentNullException>(
            () => NightProcessor.Run(TestGames.WithEnergy(150), null!, DayEndReason.Slept));
    }

    [Fact]
    public void Night_with_an_unknown_day_end_reason_throws()
    {
        var game = TestGames.WithEnergy(10);

        Assert.Throws<ArgumentOutOfRangeException>(() => NightProcessor.Run(game, TestGames.Shipped, (DayEndReason)99));
        Assert.Null(game.LastDayEnd);
        Assert.Equal(10, game.Player.Energy);
    }

    [Fact]
    public void BR_G3_advancing_a_clock_already_at_02_00_passes_out()
    {
        var game = TestGames.WithEnergy(60);
        game.AdvanceClock(DayEnd - DayStart, DayEnd); // The Game's own clamp: at 02:00 without a pass-out.
        var session = new GameSession(game, TestGames.Shipped);
        var dayEnds = new List<DayEndReason>();
        session.DayEnded += dayEnds.Add;

        session.AdvanceClock(0);

        Assert.Equal(new[] { DayEndReason.PassedOut }, dayEnds);
        Assert.Equal(DayStart, game.ClockMinutes);
        Assert.Equal(TestGames.Shipped.Tuning.PassOutEnergy, game.Player.Energy);
    }

    [Fact]
    public void BR_G3_sleep_drops_the_carried_part_minute_so_the_next_day_starts_at_exactly_06_00()
    {
        var session = TestGames.SessionWithEnergy(60);
        session.AdvanceClock(0.9); // 1.5 game minutes: 1 now, 0.5 carried.

        session.Sleep();
        session.AdvanceClock(0.3); // 0.5 game minutes: with nothing carried, still 06:00.

        Assert.Equal(DayStart, session.Game.ClockMinutes);
    }
}
