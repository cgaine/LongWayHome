#nullable enable

using System;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Art;

/// <summary>
/// Moves the art sandbox camera's centre over the ground on the <c>move_*</c> actions (the left stick, the D-pad, WASD
/// and the arrow keys), relative to the way the camera faces: right moves the view right on screen, up moves it away
/// from the camera. The centre stays over the sandbox's ground. Pure C#, unit-tested.
/// </summary>
public static class SandboxPan
{
    /// <summary>How fast a full stick or a held D-pad direction moves the centre, in metres per second.</summary>
    public const float MetresPerSecond = 6f;

    /// <summary>How far the centre may move left and right of the origin (x −25..25).</summary>
    public const float LimitX = 25f;

    /// <summary>How far back the centre may move (z −21).</summary>
    public const float MinZ = -21f;

    /// <summary>How far forward the centre may move (z 37).</summary>
    public const float MaxZ = 37f;

    /// <summary>
    /// The centre after moving for <paramref name="seconds"/> with the input <paramref name="x"/>, <paramref name="y"/>
    /// (from <c>Input.GetVector</c>: x positive = right, y negative = up) under <paramref name="view"/>, kept within
    /// x ±<see cref="LimitX"/> and z <see cref="MinZ"/>..<see cref="MaxZ"/>, over the sandbox's ground.
    /// </summary>
    public static (float X, float Z) Moved((float X, float Z) centre, float x, float y, CameraView view, float seconds)
    {
        var (dx, dz) = view.ToWorldMove(x, y);
        var step = MetresPerSecond * seconds;
        return (
            Math.Clamp(centre.X + dx * step, -LimitX, LimitX),
            Math.Clamp(centre.Z + dz * step, MinZ, MaxZ));
    }
}
