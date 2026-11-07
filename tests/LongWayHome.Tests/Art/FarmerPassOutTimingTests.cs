using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Art;

/// <summary>
/// <see cref="FarmerAnimationState.PassOutStarts"/> (#103): the pass out is timed from the clock shown plus the real
/// seconds played since it last ticked, so the clip ends about when the clock reaches 02:00 instead of being cut short
/// by the day end. It is pure presentation: the session still ends the Day at 02:00 (BR-G3).
/// </summary>
public class FarmerPassOutTimingTests
{
    private static readonly Lazy<GameTuning> ShippedTuning = new(LoadShippedTuning);

    private static float PassOutLength => FarmerMoves.For(FarmerClip.PassOut).LengthSeconds;

    [Fact]
    public void BR_G3_pass_out_starts_whenever_it_is_due_whatever_the_part_minute()
    {
        var tuning = ShippedTuning.Value;
        for (var minute = 360; minute <= 1700; minute++)
        {
            if (FarmerAnimationState.PassOutDue(minute, tuning))
            {
                Assert.True(FarmerAnimationState.PassOutStarts(minute, 0f, tuning), $"due but not started at minute {minute}");
            }
        }
    }

    [Fact]
    public void BR_G3_pass_out_starts_at_0157_once_the_clip_fills_the_true_time_left()
    {
        var tuning = ShippedTuning.Value;
        // At 01:57 shown, 1.8 s are left at most; the clip starts once the true time left reaches its length.
        var wait = FarmerAnimationState.RealSecondsUntilDayEnd(1557, tuning) - PassOutLength;

        Assert.False(FarmerAnimationState.PassOutStarts(1557, 0f, tuning));
        Assert.False(FarmerAnimationState.PassOutStarts(1557, wait - 0.05f, tuning));
        Assert.True(FarmerAnimationState.PassOutStarts(1557, wait + 0.01f, tuning));
    }

    [Theory]
    [InlineData(360)]
    [InlineData(1200)]
    [InlineData(1555)]
    public void BR_G3_pass_out_never_starts_more_than_a_minute_early(int clockMinutes)
    {
        // Even a stale part-minute (longer than a game minute) counts as at most one minute.
        Assert.False(FarmerAnimationState.PassOutStarts(clockMinutes, 100f, ShippedTuning.Value));
    }

    [Fact]
    public void BR_G3_pass_out_ignores_a_negative_part_minute()
    {
        var tuning = ShippedTuning.Value;

        Assert.Equal(
            FarmerAnimationState.PassOutStarts(1557, 0f, tuning),
            FarmerAnimationState.PassOutStarts(1557, -5f, tuning));
    }

    [Fact]
    public void BR_G3_a_sleep_clip_never_meets_a_pass_out()
    {
        // Whenever the door plays the sleep clip, the pass out isn't starting at that moment, even with the true clock a
        // whole minute ahead of the one shown (and the animator never starts it while the sleep plays).
        var tuning = ShippedTuning.Value;
        var oneMinute = FarmerAnimationState.RealSecondsUntilDayEnd(tuning.DayEndMinutes - 1, tuning);
        for (var minute = 360; minute <= 1560; minute++)
        {
            if (FarmerAnimationState.SleepPlaysClip(minute, tuning))
            {
                Assert.False(FarmerAnimationState.PassOutStarts(minute, oneMinute, tuning), $"pass out starts during a sleep at {minute}");
            }
        }
    }

    [Fact]
    public void AC2_walk_playback_scale_is_1_for_no_walk_speed()
    {
        Assert.Equal(1f, FarmerAnimationState.WalkPlaybackScale(2f, 0f));
    }

    [Fact]
    public void AC1_for_rejects_an_unknown_clip()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FarmerMoves.For((FarmerClip)999));
    }

    private static GameTuning LoadShippedTuning()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "data");
        string Read(string fileName) => File.ReadAllText(Path.Combine(folder, fileName));

        return new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName)).Load().Tuning;
    }
}
