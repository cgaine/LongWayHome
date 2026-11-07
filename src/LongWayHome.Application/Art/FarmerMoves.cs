using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>The farmer's animation clips (#103, ART_DIRECTION §6.1).</summary>
public enum FarmerClip
{
    Idle,
    Walk,
    Run,
    HoeSwing,
    Water,
    Plant,
    Harvest,
    Refill,
    Ship,
    Sleep,
    PassOut,
}

/// <summary>
/// One bone's rotation at <paramref name="Time"/> seconds into a clip, in degrees from the bone's rest (Euler YXZ, as
/// Godot's <c>RotationDegrees</c>).
/// </summary>
public sealed record FarmerKey(string Bone, float Time, float PitchDegrees, float YawDegrees, float RollDegrees);

/// <summary>The offset of <c>Hips</c> from its rest at <paramref name="Time"/> seconds into a clip, in metres.</summary>
public sealed record FarmerHipsKey(float Time, float Y, float Z);

/// <summary>One clip: its length, whether it loops, its bone keys and its <c>Hips</c> keys.</summary>
public sealed record FarmerClipSpec(
    FarmerClip Clip,
    float LengthSeconds,
    bool Loops,
    IReadOnlyList<FarmerKey> Keys,
    IReadOnlyList<FarmerHipsKey> Hips);

/// <summary>The farmer's keyframe table. Pure C#, no Godot.</summary>
public static class FarmerMoves
{
    /// <summary>The keyframes of <paramref name="clip"/>.</summary>
    public static FarmerClipSpec For(FarmerClip clip) => throw new NotImplementedException();
}

/// <summary>Which clip the farmer plays, from what he is doing. Pure presentation; no rule.</summary>
public static class FarmerAnimationState
{
    public static FarmerClip Locomotion(float horizontalSpeed, float walkSpeed, bool runHeld) =>
        throw new NotImplementedException();

    public static float WalkPlaybackScale(float horizontalSpeed, float walkSpeed) =>
        throw new NotImplementedException();

    public static FarmerClip? ForUse(UseActionKind? action) => throw new NotImplementedException();

    public static float RealSecondsUntilDayEnd(int clockMinutes, GameTuning tuning) =>
        throw new NotImplementedException();

    public static bool SleepPlaysClip(int clockMinutes, GameTuning tuning) => throw new NotImplementedException();

    public static bool PassOutDue(int clockMinutes, GameTuning tuning) => throw new NotImplementedException();
}
