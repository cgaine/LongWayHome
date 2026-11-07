using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The farmer's animation table and chooser (#103): <see cref="FarmerMoves.For"/> (the keyframes of every clip) and
/// <see cref="FarmerAnimationState"/> (which clip plays, and when the day-end clips may play without moving the end of
/// the Day, BR-G3).
/// </summary>
public class FarmerMovesTests
{
    private const float Tolerance = 1e-4f;

    private const float WalkSpeed = 3.6f;

    private const float RunSpeed = 5.2f;

    private static readonly FarmerClip[] LoopingClips = [FarmerClip.Idle, FarmerClip.Walk, FarmerClip.Run];

    public static TheoryData<FarmerClip> AllClips => new(Enum.GetValues<FarmerClip>());

    public static TheoryData<FarmerClip> Loopers => new(LoopingClips);

    public static TheoryData<FarmerClip> OneShots => new(Enum.GetValues<FarmerClip>().Except(LoopingClips));

    private static readonly Lazy<GameTuning> ShippedTuning = new(LoadShippedTuning);

    // ---- AC1: every clip, valid bones and times, seamless loops ----

    [Theory]
    [MemberData(nameof(AllClips))]
    public void AC1_for_returns_a_spec_for_every_clip(FarmerClip clip)
    {
        var spec = FarmerMoves.For(clip);

        Assert.NotNull(spec);
        Assert.Equal(clip, spec.Clip);
        Assert.True(spec.LengthSeconds > 0f, $"{clip} has length {spec.LengthSeconds}");
        Assert.NotNull(spec.Keys);
        Assert.NotNull(spec.Hips);
        Assert.NotEmpty(spec.Keys);
    }

    [Theory]
    [MemberData(nameof(AllClips))]
    public void AC1_every_key_names_a_farmer_bone(FarmerClip clip)
    {
        var bones = FarmerArt.Bones.Select(bone => bone.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var key in FarmerMoves.For(clip).Keys)
        {
            Assert.True(bones.Contains(key.Bone), $"{clip} has a key on '{key.Bone}', which is not in FarmerArt.Bones");
        }
    }

    [Theory]
    [MemberData(nameof(AllClips))]
    public void AC1_every_key_lies_within_the_clip_length(FarmerClip clip)
    {
        var spec = FarmerMoves.For(clip);

        foreach (var key in spec.Keys)
        {
            Assert.True(
                key.Time >= 0f && key.Time <= spec.LengthSeconds + Tolerance,
                $"{clip}'s {key.Bone} key at {key.Time} s is outside 0..{spec.LengthSeconds}");
        }
    }

    [Theory]
    [MemberData(nameof(AllClips))]
    public void AC1_every_hips_key_lies_within_the_clip_length(FarmerClip clip)
    {
        var spec = FarmerMoves.For(clip);

        foreach (var key in spec.Hips)
        {
            Assert.True(
                key.Time >= 0f && key.Time <= spec.LengthSeconds + Tolerance,
                $"{clip}'s Hips key at {key.Time} s is outside 0..{spec.LengthSeconds}");
        }
    }

    [Theory]
    [MemberData(nameof(Loopers))]
    public void AC1_idle_walk_and_run_loop(FarmerClip clip)
    {
        Assert.True(FarmerMoves.For(clip).Loops, $"{clip} does not loop");
    }

    [Theory]
    [MemberData(nameof(OneShots))]
    public void AC1_the_other_eight_clips_do_not_loop(FarmerClip clip)
    {
        Assert.False(FarmerMoves.For(clip).Loops, $"{clip} loops");
    }

    [Theory]
    [MemberData(nameof(Loopers))]
    public void AC1_looping_clips_first_and_last_key_per_bone_are_equal(FarmerClip clip)
    {
        foreach (var bone in FarmerMoves.For(clip).Keys.GroupBy(key => key.Bone, StringComparer.Ordinal))
        {
            var ordered = bone.OrderBy(key => key.Time).ToList();
            var first = ordered[0];
            var last = ordered[^1];

            Assert.True(
                Near(first.PitchDegrees, last.PitchDegrees) && Near(first.YawDegrees, last.YawDegrees) && Near(first.RollDegrees, last.RollDegrees),
                $"{clip}'s {bone.Key} starts at ({first.PitchDegrees}, {first.YawDegrees}, {first.RollDegrees}) " +
                $"but ends at ({last.PitchDegrees}, {last.YawDegrees}, {last.RollDegrees}), so the loop pops");
        }
    }

    [Theory]
    [MemberData(nameof(Loopers))]
    public void AC1_looping_clips_first_and_last_hips_key_are_equal(FarmerClip clip)
    {
        var hips = FarmerMoves.For(clip).Hips.OrderBy(key => key.Time).ToList();
        if (hips.Count == 0)
        {
            return;
        }

        var first = hips[0];
        var last = hips[^1];
        Assert.True(
            Near(first.Y, last.Y) && Near(first.Z, last.Z),
            $"{clip}'s Hips starts at ({first.Y}, {first.Z}) but ends at ({last.Y}, {last.Z}), so the loop pops");
    }

    [Theory]
    [InlineData(FarmerClip.Idle, 2.4f)]
    [InlineData(FarmerClip.Walk, 0.8f)]
    [InlineData(FarmerClip.Run, 0.55f)]
    [InlineData(FarmerClip.HoeSwing, 0.6f)]
    [InlineData(FarmerClip.Water, 0.7f)]
    [InlineData(FarmerClip.Plant, 0.6f)]
    [InlineData(FarmerClip.Harvest, 0.7f)]
    [InlineData(FarmerClip.Refill, 0.8f)]
    [InlineData(FarmerClip.Ship, 0.6f)]
    [InlineData(FarmerClip.Sleep, 1.4f)]
    [InlineData(FarmerClip.PassOut, 1.4f)]
    public void AC1_clip_length_is_within_30_percent_of_the_stated_default(FarmerClip clip, float defaultSeconds)
    {
        var length = FarmerMoves.For(clip).LengthSeconds;

        Assert.InRange(length, defaultSeconds * 0.7f - Tolerance, defaultSeconds * 1.3f + Tolerance);
    }

    // ---- AC2: locomotion ----

    [Theory]
    [InlineData(0f, false)]
    [InlineData(0f, true)]
    [InlineData(0.05f, false)]
    [InlineData(0.099f, true)]
    public void AC2_locomotion_is_idle_below_a_tenth_of_a_tile_per_second(float speed, bool runHeld)
    {
        Assert.Equal(FarmerClip.Idle, FarmerAnimationState.Locomotion(speed, WalkSpeed, runHeld));
    }

    [Theory]
    [InlineData(0.1f)]
    [InlineData(1.8f)]
    [InlineData(WalkSpeed)]
    public void AC2_locomotion_is_walk_when_moving_without_run(float speed)
    {
        Assert.Equal(FarmerClip.Walk, FarmerAnimationState.Locomotion(speed, WalkSpeed, runHeld: false));
    }

    [Fact]
    public void AC2_locomotion_is_walk_above_walk_speed_without_run_held()
    {
        Assert.Equal(FarmerClip.Walk, FarmerAnimationState.Locomotion(RunSpeed, WalkSpeed, runHeld: false));
    }

    [Theory]
    [InlineData(RunSpeed)]
    [InlineData(4f)]
    public void AC2_locomotion_is_run_with_run_held_above_walk_speed(float speed)
    {
        Assert.Equal(FarmerClip.Run, FarmerAnimationState.Locomotion(speed, WalkSpeed, runHeld: true));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2.6f)]
    [InlineData(WalkSpeed)]
    public void AC2_locomotion_is_walk_with_run_held_but_a_slow_analog_tilt(float speed)
    {
        Assert.Equal(FarmerClip.Walk, FarmerAnimationState.Locomotion(speed, WalkSpeed, runHeld: true));
    }

    [Theory]
    [InlineData(WalkSpeed, 1f)]
    [InlineData(1.8f, 0.5f)]
    [InlineData(2.52f, 0.7f)]
    [InlineData(1.44f, 0.4f)]
    public void AC2_walk_playback_scale_is_speed_over_walk_speed(float speed, float expected)
    {
        Assert.Equal(expected, FarmerAnimationState.WalkPlaybackScale(speed, WalkSpeed), Tolerance);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.36f)]
    [InlineData(1f)]
    public void AC2_walk_playback_scale_is_at_least_0_4(float speed)
    {
        Assert.Equal(0.4f, FarmerAnimationState.WalkPlaybackScale(speed, WalkSpeed), Tolerance);
    }

    [Theory]
    [InlineData(RunSpeed)]
    [InlineData(7.2f)]
    [InlineData(100f)]
    public void AC2_walk_playback_scale_is_at_most_1(float speed)
    {
        Assert.Equal(1f, FarmerAnimationState.WalkPlaybackScale(speed, WalkSpeed), Tolerance);
    }

    // ---- AC3: Use ----

    [Theory]
    [InlineData(UseActionKind.Till, FarmerClip.HoeSwing)]
    [InlineData(UseActionKind.Water, FarmerClip.Water)]
    [InlineData(UseActionKind.Plant, FarmerClip.Plant)]
    [InlineData(UseActionKind.Harvest, FarmerClip.Harvest)]
    public void AC3_for_use_maps_each_action_to_its_clip(UseActionKind action, FarmerClip expected)
    {
        Assert.Equal(expected, FarmerAnimationState.ForUse(action));
    }

    [Fact]
    public void AC3_for_use_of_no_action_is_no_clip()
    {
        Assert.Null(FarmerAnimationState.ForUse(null));
    }

    // ---- AC4: real seconds until the day ends, with the shipped tuning ----

    [Theory]
    [InlineData(360, 720f)]
    [InlineData(1558, 1.2f)]
    [InlineData(1559, 0.6f)]
    [InlineData(1560, 0f)]
    public void AC4_real_seconds_until_day_end_with_the_shipped_tuning(int clockMinutes, float expected)
    {
        Assert.Equal(expected, FarmerAnimationState.RealSecondsUntilDayEnd(clockMinutes, ShippedTuning.Value), Tolerance);
    }

    [Theory]
    [InlineData(1561)]
    [InlineData(1600)]
    [InlineData(2000)]
    public void AC4_real_seconds_until_day_end_is_never_below_zero(int clockMinutes)
    {
        Assert.Equal(0f, FarmerAnimationState.RealSecondsUntilDayEnd(clockMinutes, ShippedTuning.Value), Tolerance);
    }

    // ---- BR-G3: the day-end clips never move the end of the Day ----

    [Fact]
    public void BR_G3_sleep_plays_its_clip_at_0156()
    {
        Assert.True(FarmerAnimationState.SleepPlaysClip(1556, ShippedTuning.Value));
    }

    [Fact]
    public void BR_G3_sleep_plays_its_clip_through_the_day_up_to_0156()
    {
        for (var minute = 360; minute <= 1556; minute++)
        {
            Assert.True(FarmerAnimationState.SleepPlaysClip(minute, ShippedTuning.Value), $"no sleep clip at minute {minute}");
        }
    }

    [Theory]
    [InlineData(1557)]
    [InlineData(1558)]
    [InlineData(1559)]
    [InlineData(1560)]
    [InlineData(1600)]
    public void BR_G3_sleep_plays_no_clip_from_0157(int clockMinutes)
    {
        Assert.False(FarmerAnimationState.SleepPlaysClip(clockMinutes, ShippedTuning.Value));
    }

    /// <summary>The clock shown can be up to a game minute behind the true clock, so the clip must end before 02:00
    /// even from the start of the next minute.</summary>
    [Fact]
    public void BR_G3_sleep_clip_never_outlasts_the_time_left_even_a_minute_late()
    {
        var tuning = ShippedTuning.Value;
        var sleepLength = FarmerMoves.For(FarmerClip.Sleep).LengthSeconds;

        for (var minute = 0; minute <= 1700; minute++)
        {
            if (!FarmerAnimationState.SleepPlaysClip(minute, tuning))
            {
                continue;
            }

            var left = FarmerAnimationState.RealSecondsUntilDayEnd(minute + 1, tuning);
            Assert.True(left > sleepLength, $"at minute {minute} the {sleepLength} s sleep clip plays with only {left} s left");
        }
    }

    [Theory]
    [InlineData(360)]
    [InlineData(1200)]
    [InlineData(1556)]
    public void BR_G3_pass_out_is_not_due_before_the_last_seconds(int clockMinutes)
    {
        Assert.False(FarmerAnimationState.PassOutDue(clockMinutes, ShippedTuning.Value));
    }

    [Theory]
    [InlineData(1558)]
    [InlineData(1559)]
    [InlineData(1560)]
    [InlineData(1561)]
    [InlineData(1700)]
    public void BR_G3_pass_out_is_due_from_0158_including_at_and_after_0200(int clockMinutes)
    {
        Assert.True(FarmerAnimationState.PassOutDue(clockMinutes, ShippedTuning.Value));
    }

    [Fact]
    public void BR_G3_pass_out_is_due_exactly_when_the_clip_fills_the_time_left()
    {
        var tuning = ShippedTuning.Value;
        var passOutLength = FarmerMoves.For(FarmerClip.PassOut).LengthSeconds;

        for (var minute = 360; minute <= 1700; minute++)
        {
            var expected = FarmerAnimationState.RealSecondsUntilDayEnd(minute, tuning) <= passOutLength;
            Assert.Equal(expected, FarmerAnimationState.PassOutDue(minute, tuning));
        }
    }

    // ---- helpers ----

    private static bool Near(float a, float b) => Math.Abs(a - b) <= Tolerance;

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
